using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PCLStorage;
using QuickShare.Common;
using QuickShare.Common.Extensions;
using QuickShare.Common.Interfaces;
using QuickShare.DataStore;
using QuickShare.FileTransfer.Helpers;

namespace QuickShare.FileTransfer
{
    class ReceiveSessionAgent
    {
        static readonly int numberOfParallelDownloads = 4;

        public delegate void ReceiveFileProgressEventHandler(FileTransfer2ProgressEventArgs e);
        public event ReceiveFileProgressEventHandler FileTransferProgress;

        public TaskCompletionSource<bool> ReceiveFinishTcs { get; private set; }

        string ip;
        Guid sessionKey;
        string senderName;
        IDownloadFolderDecider downloadFolderDecider;
        CancellationTokenSource cancellationTokenSource;
        FileReceiveProgressCalculator progressCalculator;

        public ReceiveSessionAgent(string ip, Guid sessionKey, string senderName, IDownloadFolderDecider downloadFolderDecider)
        {
            this.ip = ip;
            this.sessionKey = sessionKey;
            this.senderName = senderName;
            this.downloadFolderDecider = downloadFolderDecider;

            ReceiveFinishTcs = new TaskCompletionSource<bool>();
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async void StartReceive(bool isResume)
        {
            try
            {
                var cancellationToken = cancellationTokenSource.Token;

                var queueInfo = await GetQueueInfoAsync(ip, sessionKey, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    throw new ReceiveCancelledException();

                var downloadFolder = await downloadFolderDecider.DecideAsync(queueInfo.Files.Select(x => Path.GetExtension(x.FileName)).ToArray());
                await StartDownload(queueInfo, senderName, ip, sessionKey, downloadFolder, isResume, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                ReceiveFinishTcs.TrySetResult(false);
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        private static async Task AddToHistory(QueueInfo queueInfo, Guid sessionKey, string senderName, IFolder downloadRootFolder)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Add(sessionKey,
                DateTime.Now,
                senderName,
                new ReceivedFileCollection
                {
                    Files = queueInfo.Files.Select(x => new ReceivedFile
                    {
                        Name = x.FileName,
                        OriginalName = x.FileName,
                        Size = (long)x.FileSize,
                        StorePath = Path.Combine(downloadRootFolder.Path, NormalizePathForCombine(x.RelativePath)),
                        Completed = false,
                        LastSliceReceived = null,
                    }).ToList(),
                    StoreRootPath = downloadRootFolder.Path,
                },
                false);
            DataStorageProviders.HistoryManager.Close();
        }

        private async Task StartDownload(QueueInfo queueInfo, string senderName, string ip, Guid sessionKey, IFolder downloadRootFolder, bool isResume, CancellationToken cancellationToken)
        {
            if (!isResume)
                await AddToHistory(queueInfo, sessionKey, senderName, downloadRootFolder);
            progressCalculator = new FileReceiveProgressCalculator(queueInfo, queueInfo.Files.First().SliceMaxLength, senderName, sessionKey);
            progressCalculator.FileTransferProgress += ProgressCalculator_FileTransferProgress;

            await queueInfo.Files.ParallelForEachAsync(numberOfParallelDownloads, async item =>
            {
                await DownloadFile(item, downloadRootFolder, ip, sessionKey, cancellationToken);
            });

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.ChangeCompletedStatus(sessionKey, true);
            DataStorageProviders.HistoryManager.Close();

            await SendFinishGetRequestAsync(ip, sessionKey);

            FileTransferProgress?.Invoke(new FileTransfer2ProgressEventArgs
            {
                State = FileTransferState.Finished,
                Guid = sessionKey,
                TotalFiles = queueInfo.Files.Count,
                SenderName = senderName,
            });

            ReceiveFinishTcs.SetResult(true);
        }

        private void ProgressCalculator_FileTransferProgress(object sender, FileTransfer2ProgressEventArgs e)
        {
            FileTransferProgress?.Invoke(e);
        }

        private async Task DownloadFile(FileSendInfo fileInfo, IFolder downloadRootFolder, string serverIp, Guid sessionKey, CancellationToken cancellationToken)
        {
            //TODO: Add a semaphore or sth to make sure two threads don't start writing on one file.

            IFolder downloadFolder = await FileHelper.CreateDirectoryIfNecessary(downloadRootFolder, fileInfo.RelativePath);

            await DataStorageProviders.HistoryManager.OpenAsync();
            var origFile = DataStorageProviders.HistoryManager.GetFileFromOriginalName(guid: sessionKey, originalFileName: fileInfo.FileName, path: downloadFolder.Path);
            DataStorageProviders.HistoryManager.Close();

            IFile file;
            uint firstSliceToReceive = 0;
            if (origFile.LastSliceReceived == null)
            {
                file = await FileHelper.CreateFile(downloadFolder, fileInfo.FileName);

                if (file.Name != fileInfo.FileName) //File already existed, so new name generated for it. We should update database now.
                {
                    await DataStorageProviders.HistoryManager.OpenAsync();
                    DataStorageProviders.HistoryManager.UpdateFileName(sessionKey, fileInfo.FileName, file.Name, downloadFolder.Path);
                    DataStorageProviders.HistoryManager.Close();
                }
            }
            else
            {
                file = await FileHelper.GetFile(downloadFolder, origFile.Name);

                firstSliceToReceive = (uint)(origFile.LastSliceReceived) + 1;
                progressCalculator.SliceReceived(fileInfo, (uint)(origFile.LastSliceReceived));
            }

            ulong totalBytesReceived = 0;
            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                stream.Seek(stream.Length, SeekOrigin.Begin);
                bool b = false;
                for (uint i = firstSliceToReceive; i < fileInfo.SlicesCount; i++)
                {
                    string url = $"http://{serverIp}:{Constants.CommunicationPort}/{fileInfo.UniqueKey}/{i}/";

                    if (cancellationToken.IsCancellationRequested)
                        throw new ReceiveCancelledException();

                    byte[] buffer = await HttpHelper.DownloadDataFromUrl(url, cancellationToken: cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                        throw new ReceiveCancelledException();

                    int expectedLength;
                    if (i == (fileInfo.SlicesCount - 1))
                        expectedLength = (int)(fileInfo.FileSize % fileInfo.SliceMaxLength);
                    else
                        expectedLength = (int)fileInfo.SliceMaxLength;

                    if (buffer.Length != expectedLength)
                    {
                        Debug.WriteLine("Slice length violation! Will retry...");
                        i--;
                        continue;
                    }
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    await stream.FlushAsync();

                    totalBytesReceived += (ulong)expectedLength;

                    progressCalculator.SliceReceived(fileInfo, i);

                    await DataStorageProviders.HistoryManager.OpenAsync();
                    DataStorageProviders.HistoryManager.SetFileLastSliceReceived(sessionKey, file.Name, downloadFolder.Path, i);
                    DataStorageProviders.HistoryManager.Close();

                    if (b)
                        throw new Exception();
                }
            }

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.MarkFileAsCompleted(sessionKey, file.Name, downloadFolder.Path);
            DataStorageProviders.HistoryManager.Close();
        }

        private async Task<QueueInfo> GetQueueInfoAsync(string ip, Guid sessionKey, CancellationToken cancellationToken)
        {
            string data = await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/queueInfo/", 
                cancellationToken: cancellationToken);
            return JsonConvert.DeserializeObject<QueueInfo>(data);
        }

        private static string NormalizePathForCombine(string path)
        {
            if (path.Length == 0)
                return path;

            return (path[0] == '\\' || path[0] == '/') ? path.Substring(1) : path;
        }

        private static async Task SendFinishGetRequestAsync(string ip, Guid sessionKey)
        {
            try
            {
                await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/finishQueue/");
            }
            catch { }
        }
    }
}
