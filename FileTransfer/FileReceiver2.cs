using FileTransfer.Helpers;
using Newtonsoft.Json;
using PCLStorage;
using QuickShare.Common;
using QuickShare.DataStore;
using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer
{
    public static class FileReceiver2
    {
        static readonly int fileReceiverVersion = 2;
        static readonly TimeSpan downloadTimeout = TimeSpan.FromSeconds(5);
        static readonly int downloadMaxTryCount = 3;

        public delegate void ReceiveFileProgressEventHandler(FileTransfer2ProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static FileReceiveProgressCalculator progressCalculator;

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            int fileSenderVersion = 2;
            // FileSender v1
            if (!request.ContainsKey("FileSenderVersion") || (int.Parse(request["FileSenderVersion"].ToString()) < 2))
            {
                fileSenderVersion = 1;
                await ProcessRequestLegacy(request, fileSenderVersion, downloadFolderDecider);
                return new Dictionary<string, object>();
            }

            await ProcessRequest(request, fileSenderVersion, downloadFolderDecider);
            return new Dictionary<string, object>();
        }

        private static Task ProcessRequestLegacy(Dictionary<string, object> request, int fileSenderVersion, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            throw new NotImplementedException();
        }

        private static async Task ProcessRequest(Dictionary<string, object> request, int fileSenderVersion, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            var sessionGuid = Guid.Parse(request["Guid"] as string);
            var sessionKey = request["QueueInfoKey"].ToString();
            var ip = request["ServerIP"].ToString();
            var isCompatible = CompatibilityHelper.IsCompatible(fileSenderVersion, fileReceiverVersion);
            var senderName = request["SenderName"].ToString();

            if (!isCompatible)
            {
                // TODO
                return;
            }

            await SendVersionCheckGetRequestAsync(ip, sessionKey, isCompatible);
            var queueInfo = await GetQueueInfoAsync(ip, sessionKey);

            var downloadFolder = await downloadFolderDecider(queueInfo.Files.Select(x => Path.GetExtension(x.FileName)).ToArray());

            await StartDownload(queueInfo, senderName, ip, sessionKey, sessionGuid, downloadFolder);
        }

        private static async Task AddToHistory(QueueInfo queueInfo, Guid sessionGuid, string senderName, IFolder downloadRootFolder)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Add(sessionGuid,
                DateTime.Now,
                senderName,
                new ReceivedFileCollection
                {
                    Files = queueInfo.Files.Select(x => new ReceivedFile
                    {
                        Name = x.FileName,
                        Size = (long)x.FileSize,
                        StorePath = Path.Combine(downloadRootFolder.Path, x.RelativePath),
                        Completed = false,
                    }).ToList(),
                    StoreRootPath = downloadRootFolder.Path,
                },
                false);
            DataStorageProviders.HistoryManager.Close();
        }

        private static async Task StartDownload(QueueInfo queueInfo, string senderName, string ip, string sessionKey, Guid sessionGuid, IFolder downloadRootFolder)
        {
            await AddToHistory(queueInfo, sessionGuid, senderName, downloadRootFolder);
            progressCalculator = new FileReceiveProgressCalculator(queueInfo, queueInfo.Files.First().SliceMaxLength);
            progressCalculator.FileTransferProgress += ProgressCalculator_FileTransferProgress;

            // TODO: Parallelize this
            foreach (var item in queueInfo.Files)
            {
                await DownloadFile(item, downloadRootFolder, ip, sessionKey, sessionGuid);
            }

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.ChangeCompletedStatus(sessionGuid, true);
            DataStorageProviders.HistoryManager.Close();

            FileTransferProgress?.Invoke(new FileTransfer2ProgressEventArgs
            {
                State = FileTransferState.Finished,
            });
        }

        private static void ProgressCalculator_FileTransferProgress(object sender, FileTransfer2ProgressEventArgs e)
        {
            FileTransferProgress?.Invoke(e);
        }

        private static async Task DownloadFile(FileSendInfo fileInfo, IFolder downloadRootFolder, string serverIp, string sessionKey, Guid sessionGuid)
        {
            //TODO: Add a semaphore or sth to make sure two threads don't start writing on one file.

            IFolder downloadFolder = await FileHelper.CreateDirectoryIfNecessary(downloadRootFolder, fileInfo.RelativePath);

            IFile file = await FileHelper.CreateFile(downloadFolder, fileInfo.FileName);

            if (file.Name != fileInfo.FileName) //File already existed, so new name generated for it. We should update database now.
            {
                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.UpdateFileName(sessionGuid, fileInfo.FileName, file.Name, downloadFolder.Path);
                DataStorageProviders.HistoryManager.Close();
            }

            ulong totalBytesReceived = 0;
            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                for (uint i = 0; i < fileInfo.SlicesCount; i++)
                {
                    string url = $"http://{serverIp}:{Constants.CommunicationPort}/{sessionKey}/{i}/";

                    byte[] buffer = await HttpHelper.DownloadDataFromUrl(url, downloadTimeout, downloadMaxTryCount);

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
                    totalBytesReceived += (ulong)expectedLength;
                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    progressCalculator.SliceReceived(fileInfo, i);
                }

                await stream.FlushAsync();
            }

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.MarkFileAsCompleted(sessionGuid, file.Name, downloadFolder.Path);
            DataStorageProviders.HistoryManager.Close();
        }

        private static async Task<QueueInfo> GetQueueInfoAsync(string ip, string sessionKey)
        {
            return JsonConvert.DeserializeObject<QueueInfo>(await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey}/queueInfo/"));
        }

        private static async Task SendVersionCheckGetRequestAsync(string ip, string sessionKey, bool isCompatible)
        {
            try
            {
                await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey}/versionCheck/?receiverVersion={fileReceiverVersion}&receiverCompatible={(isCompatible ? "true" : "false")}");
            }
            catch { }
        }

        
    }
}
