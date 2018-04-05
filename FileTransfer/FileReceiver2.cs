using QuickShare.FileTransfer.Helpers;
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
using QuickShare.Common.Extensions;
using QuickShare.FileTransfer.Exceptions;
using QuickShare.Common.Interfaces;

namespace QuickShare.FileTransfer
{
    public static class FileReceiver2
    {
        static readonly int fileReceiverVersion = 2;
        static readonly int numberOfParallelDownloads = 4;

        public delegate void ReceiveFileProgressEventHandler(FileTransfer2ProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static FileReceiveProgressCalculator progressCalculator;

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, IDownloadFolderDecider downloadFolderDecider)
        {
            try
            {
                int fileSenderVersion = 2;
                // FileSender v1
                if (!request.ContainsKey("FileSenderVersion") || (int.Parse(request["FileSenderVersion"].ToString()) < 2))
                {
                    fileSenderVersion = 1;
                    return await ProcessRequestLegacy(request, fileSenderVersion, downloadFolderDecider);
                }

                if (!request.ContainsKey("Type"))
                    throw new InvalidOperationException("Field 'Type' is missing from request.");

                switch (request["Type"] as string)
                {
                    case "QueueInit":
                        await ProcessRequest(request, fileSenderVersion, downloadFolderDecider);
                        return new Dictionary<string, object>();
                    case "ResumeReceive":
                        //TODO
                        Debug.WriteLine("Received ResumeReceive request. TODO.");
                        return new Dictionary<string, object>
                    {
                        { "Accepted", "true" },
                    };
                    default:
                        throw new InvalidOperationException($"Type '{request["Type"]}' is invalid.");
                }
            }
            catch (Exception ex)
            {
                FileTransferProgress?.Invoke(new FileTransfer2ProgressEventArgs {
                    State = FileTransferState.Error,
                    Exception = ex,
                });
                throw ex;
            }
        }

        private static async Task<Dictionary<string, object>> ProcessRequestLegacy(Dictionary<string, object> request, int fileSenderVersion, IDownloadFolderDecider downloadFolderDecider)
        {
            FileReceiver.ClearEventRegistrations();
            FileReceiver.FileTransferProgress += LegacyFileReceiver_FileTransferProgress;

            return await FileReceiver.ReceiveRequest(request, downloadFolderDecider);
        }

        private static void LegacyFileReceiver_FileTransferProgress(FileTransferProgressEventArgs e)
        {
            FileTransferProgress?.Invoke(new FileTransfer2ProgressEventArgs
            {
                Guid = e.Guid,
                SenderName = e.SenderName,
                State = e.State,
                TotalBytes = e.Total * Constants.FileSliceMaxLength,
                TotalTransferredBytes = e.CurrentPart * Constants.FileSliceMaxLength,
                TotalFiles = e.TotalFiles,
            });
        }

        private static async Task ProcessRequest(Dictionary<string, object> request, int fileSenderVersion, IDownloadFolderDecider downloadFolderDecider)
        {
            var sessionKey = Guid.Parse(request["Guid"] as string);
            var ip = request["ServerIP"].ToString();
            var isCompatible = CompatibilityHelper.IsCompatible(fileSenderVersion, fileReceiverVersion);
            var senderName = request["SenderName"].ToString();

            if (fileSenderVersion >= 2)
                await SendVersionCheckGetRequestAsync(ip, sessionKey, isCompatible);

            if (!isCompatible)
            {
                // TODO
                return;
            }
            
            var queueInfo = await GetQueueInfoAsync(ip, sessionKey);
            var downloadFolder = await downloadFolderDecider.DecideAsync(queueInfo.Files.Select(x => Path.GetExtension(x.FileName)).ToArray());
            await StartDownload(queueInfo, senderName, ip, sessionKey, downloadFolder);
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
                        Size = (long)x.FileSize,
                        StorePath = Path.Combine(downloadRootFolder.Path, x.RelativePath),
                        Completed = false,
                    }).ToList(),
                    StoreRootPath = downloadRootFolder.Path,
                },
                false);
            DataStorageProviders.HistoryManager.Close();
        }

        private static async Task StartDownload(QueueInfo queueInfo, string senderName, string ip, Guid sessionKey, IFolder downloadRootFolder)
        {
            await AddToHistory(queueInfo, sessionKey, senderName, downloadRootFolder);
            progressCalculator = new FileReceiveProgressCalculator(queueInfo, queueInfo.Files.First().SliceMaxLength, senderName, sessionKey);
            progressCalculator.FileTransferProgress += ProgressCalculator_FileTransferProgress;

            await queueInfo.Files.ParallelForEachAsync(numberOfParallelDownloads, async item =>
            {
                await DownloadFile(item, downloadRootFolder, ip, sessionKey);
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
        }

        private static void ProgressCalculator_FileTransferProgress(object sender, FileTransfer2ProgressEventArgs e)
        {
            FileTransferProgress?.Invoke(e);
        }

        private static async Task DownloadFile(FileSendInfo fileInfo, IFolder downloadRootFolder, string serverIp, Guid sessionKey)
        {
            //TODO: Add a semaphore or sth to make sure two threads don't start writing on one file.

            IFolder downloadFolder = await FileHelper.CreateDirectoryIfNecessary(downloadRootFolder, fileInfo.RelativePath);

            IFile file = await FileHelper.CreateFile(downloadFolder, fileInfo.FileName);

            if (file.Name != fileInfo.FileName) //File already existed, so new name generated for it. We should update database now.
            {
                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.UpdateFileName(sessionKey, fileInfo.FileName, file.Name, downloadFolder.Path);
                DataStorageProviders.HistoryManager.Close();
            }

            ulong totalBytesReceived = 0;
            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                for (uint i = 0; i < fileInfo.SlicesCount; i++)
                {
                    string url = $"http://{serverIp}:{Constants.CommunicationPort}/{fileInfo.UniqueKey}/{i}/";

                    byte[] buffer = await HttpHelper.DownloadDataFromUrl(url);

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
            DataStorageProviders.HistoryManager.MarkFileAsCompleted(sessionKey, file.Name, downloadFolder.Path);
            DataStorageProviders.HistoryManager.Close();
        }

        private static async Task<QueueInfo> GetQueueInfoAsync(string ip, Guid sessionKey)
        {
            string data = await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/queueInfo/");
            return JsonConvert.DeserializeObject<QueueInfo>(data);
        }

        private static async Task SendVersionCheckGetRequestAsync(string ip, Guid sessionKey, bool isCompatible)
        {
            try
            {
                await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/versionCheck/?receiverVersion={fileReceiverVersion}&receiverCompatible={(isCompatible ? "true" : "false")}");
            }
            catch { }
        }

        private static async Task SendFinishGetRequestAsync(string ip, Guid sessionKey)
        {
            try
            {
                await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/finishQueue/");
            }
            catch { }
        }

        public static void ClearEventRegistrations()
        {
            FileTransferProgress = null;
        }

        internal static void InitHandshakerEvents()
        {
            ServerIPFinder.ClearEventRegistrations();
            ServerIPFinder.IPDetectionFailed += ServerIPFinder_IPDetectionFailed;
        }

        private static void ServerIPFinder_IPDetectionFailed()
        {
            FileTransferProgress?.Invoke(new FileTransfer2ProgressEventArgs
            {
                State = FileTransferState.Error,
                Exception = new HandshakeFailedException(),
                Guid = Guid.NewGuid(),
            });
        }
    }
}
