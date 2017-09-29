using Newtonsoft.Json;
using PCLStorage;
using QuickShare.Common;
using QuickShare.DataStore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    public static class FileReceiver
    {
        public delegate void ReceiveFileProgressEventHandler(FileTransferProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static FileReceiveState ReceiveState = null;

        static Guid uniqueKey = Guid.NewGuid();

        static DateTime lastDownload = DateTime.MinValue;

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            Dictionary<string, object> returnVal = null;

            if ((request.ContainsKey("Type")) && (request["Type"] as string == "QueueInit"))
            {
                //Queue initialization
                ReceiveState = new FileReceiveState(await GetStateStoreFolder(downloadFolderDecider))
                {
                    IsQueue = true,
                    QueueTotalSlices = (long)request["TotalSlices"],
                    QueueSlicesFinished = 0,
                    QueuedSlicesYet = 0,
                    QueueFinishUrl = "http://" + (request["ServerIP"] as string) + ":" + Constants.CommunicationPort + "/" + request["QueueFinishKey"] + "/finishQueue/",
                    FilesCount = 0,
                    QueueParentDirectory = (string)request["parentDirectoryName"],
                    LatestReceivedQueueItemGroupId = -1,
                    RequestGuid = Guid.Parse(request["Guid"] as string),
                    SenderName = (string)request["SenderName"],
                };

                returnVal = new Dictionary<string, object>
                {
                    { "QueueInitialized", "1" }
                };

                /* TODO: Do this from outside of this function */
                //await request.SendResponseAsync(vs);

                InvokeProgressEvent(0, 0, FileTransferState.QueueList);
            }
            else if ((request.ContainsKey("IsQueueItemGroup")) && (request["IsQueueItemGroup"] as string == "true"))
            {
                int partNum = int.Parse(request["PartNum"].ToString());
                Debug.WriteLine($"Received QueueItemGroup #{partNum}.");
                if (partNum > ReceiveState.LatestReceivedQueueItemGroupId)
                {
                    ReceiveState.LatestReceivedQueueItemGroupId = partNum;

                    var items = JsonConvert.DeserializeObject<List<string>>(request["Data"].ToString());

                    Debug.WriteLine($"It contains {items.Count} entries. Processing...");

                    foreach (var item in items)
                    {
                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(item);
                        await ProcessQueueItem(info, downloadFolderDecider, uniqueKey);
                    }

                    Debug.WriteLine("Processed QueueItemGroup successfully.");
                }
            }
            else if ((request.ContainsKey("IsQueueItem")) && (request["IsQueueItem"] as string == "true"))
            {
                await ProcessQueueItem(request, downloadFolderDecider, uniqueKey);
            }
            else if ((request.ContainsKey("Type")) && (request["Type"] as string == "EmergencyRetry"))
            {
                if ((DateTime.UtcNow - lastDownload) > TimeSpan.FromSeconds(7))
                {
                    lastDownload = DateTime.UtcNow;

                    uniqueKey = Guid.NewGuid();

                    var requestGuid = Guid.Parse(request["Guid"].ToString());
                    Debug.WriteLine($"Received emergency retry request for {requestGuid}");

                    ReceiveState = await FileReceiveState.LoadState(requestGuid, await GetStateStoreFolder(downloadFolderDecider));
                    Debug.WriteLine($"State recovered.");

                    if (ReceiveState.IsQueue)
                    {
                        Debug.WriteLine($"Resuming queue...");
                    }
                    else
                    {
                        Debug.WriteLine($"Resuming single file download...");
                        var downloadFolder = await downloadFolderDecider(new string[] { Path.GetExtension((string)ReceiveState.QueueItems[0]["FileName"]) });

                        ReceiveState.Downloading.Remove(ReceiveState.QueueItems[0]["DownloadKey"].ToString());
                        await DownloadFile(ReceiveState.QueueItems[0], downloadFolder, uniqueKey);


                        await DataStorageProviders.HistoryManager.OpenAsync();
                        DataStorageProviders.HistoryManager.ChangeCompletedStatus(ReceiveState.RequestGuid, true);
                        DataStorageProviders.HistoryManager.Close();
                    }
                }
            }
            else
            {
                //Singular file
                ReceiveState = new FileReceiveState(await GetStateStoreFolder(downloadFolderDecider))
                {
                    FilesCount = 1,
                    RequestGuid = Guid.Parse(request["Guid"] as string),
                    SenderName = (string)request["SenderName"],
                    IsQueue = false,
                };
                ReceiveState.QueueItems.Add(request);

                var downloadFolder = await downloadFolderDecider(new string[] { Path.GetExtension((string)ReceiveState.QueueItems[0]["FileName"]) });

                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.Add(ReceiveState.RequestGuid,
                    DateTime.Now,
                    ReceiveState.SenderName,
                    new ReceivedFileCollection
                    {
                        Files = new List<ReceivedFile>()
                        {
                            new ReceivedFile
                            {
                                Name = (string)ReceiveState.QueueItems[0]["FileName"],
                                Size = (long)ReceiveState.QueueItems[0]["FileSize"],
                                StorePath = System.IO.Path.Combine(downloadFolder.Path, (string)ReceiveState.QueueItems[0]["Directory"]),
                            }
                        },
                        StoreRootPath = System.IO.Path.Combine(downloadFolder.Path, (string)ReceiveState.QueueItems[0]["Directory"]),
                    },
                    false);
                DataStorageProviders.HistoryManager.Close();

                await DownloadFile(ReceiveState.QueueItems[0], downloadFolder, uniqueKey);

                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.ChangeCompletedStatus(ReceiveState.RequestGuid, true);
                DataStorageProviders.HistoryManager.Close();
            }

            return returnVal;
        }

        private static async Task<IFolder> GetStateStoreFolder(Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            return await downloadFolderDecider(new string[] { "___State" });
        }

        private static async Task ProcessQueueItem(Dictionary<string, object> request, Func<string[], Task<IFolder>> downloadFolderDecider, Guid _uniqueKey)
        {
            //Queue data details
            ReceiveState.QueuedSlicesYet += (int)(long)request["SlicesCount"];
            ReceiveState.QueueItems.Add(request);

            ReceiveState.FilesCount++;

            if (ReceiveState.QueuedSlicesYet == ReceiveState.QueueTotalSlices)
                await BeginProcessingQueue(downloadFolderDecider, _uniqueKey);
            else if (ReceiveState.QueuedSlicesYet > ReceiveState.QueueTotalSlices)
            {
                Debug.WriteLine("Queued more slices than expected.");
                throw new Exception("Queued more slices than expected.");
            }
        }

        public static void ClearEventRegistrations()
        {
            FileTransferProgress = null;
        }

        private static async Task BeginProcessingQueue(Func<string[], Task<IFolder>> downloadFolderDecider, Guid _uniqueKey)
        {
            var downloadFolder = await downloadFolderDecider(ReceiveState.QueueItems.Select(x => Path.GetExtension((string)x["FileName"])).ToArray());

            var logItems = from x in ReceiveState.QueueItems
                           select new ReceivedFile
                           {
                               Name = (string)x["FileName"],
                               Size = (long)x["FileSize"],
                               StorePath = System.IO.Path.Combine(downloadFolder.Path, (string)x["Directory"]),
                           };

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Add(ReceiveState.RequestGuid,
                DateTime.Now,
                ReceiveState.SenderName,
                new ReceivedFileCollection
                {
                    Files = logItems.ToList(),
                    StoreRootPath = System.IO.Path.Combine(downloadFolder.Path, ReceiveState.QueueParentDirectory),
                },
                false);
            DataStorageProviders.HistoryManager.Close();

            foreach (var item in ReceiveState.QueueItems)
            {
                await DownloadFile(item, downloadFolder, _uniqueKey);
            }

            FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = (ulong)ReceiveState.QueueTotalSlices, Total = (ulong)ReceiveState.QueueTotalSlices, State = FileTransferState.Finished, Guid = ReceiveState.RequestGuid, SenderName = ReceiveState.SenderName, TotalFiles = ReceiveState.FilesCount });

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.ChangeCompletedStatus(ReceiveState.RequestGuid, true);
            DataStorageProviders.HistoryManager.Close();

            await QueueProcessFinishedNotifySender();
        }

        private static async Task QueueProcessFinishedNotifySender()
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync(ReceiveState.QueueFinishUrl + "?success=true");
            }
            catch { }
        }

        public static async Task DownloadFile(Dictionary<string, object> message, IFolder downloadMainFolder, Guid _uniqueKey)
        {
            var key = (string)message["DownloadKey"];

            if (ReceiveState.Downloading.Contains(key))
                return;

            ReceiveState.Downloading.Add(key);

            Debug.WriteLine("Receive begin");

            int slicesCount;
            if (message["SlicesCount"] is Int64)
                slicesCount = (int)(long)message["SlicesCount"];
            else
                slicesCount = (int)message["SlicesCount"];
            var fileName = (string)message["FileName"];
            var dateModifiedMilliseconds = (long)message["DateModified"];
            var dateCreatedMilliseconds = (long)message["DateCreated"];
            var fileSize = (long)message["FileSize"];
            var directory = (string)message["Directory"];
            var serverIP = (string)message["ServerIP"];

            var dateModified = DateTimeExtension.FromUnixTimeMilliseconds(dateModifiedMilliseconds);
            var dateCreated = DateTimeExtension.FromUnixTimeMilliseconds(dateCreatedMilliseconds);

            if (downloadMainFolder == null)
            {
                await ReceiveFailed(serverIP, key, "Default downloads folder hasn't been set.");
                return;
            }

            IFolder downloadFolder = await CreateDirectoryIfNecessary(downloadMainFolder, directory);

            if (uniqueKey != _uniqueKey)
                return;

            IFile file;

            if (!message.ContainsKey("_State"))
                file = await CreateFile(downloadFolder, fileName);
            else if (message["_State"].ToString() == "Downloading")
                file = await CreateOrOpenFile(downloadFolder, message["_ReceiveFileName"].ToString());
            else
                return;

            if (file.Name != fileName) //File already existed, so new name generated for it. We should update database now.
            {
                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.UpdateFileName(ReceiveState.RequestGuid, fileName, file.Name, System.IO.Path.Combine(downloadFolder.Path, directory));
                DataStorageProviders.HistoryManager.Close();
            }

            if (!message.ContainsKey("_State"))
            {
                message["_State"] = "Downloading";
                message["_ReceiveFileName"] = file.Name;
                message["_PartsWritten"] = 0;
            }

            await ReceiveState.SaveState();

            for (int i = int.Parse(message["_PartsWritten"].ToString()); i < slicesCount; i++)
            {
                string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";
                byte[] buffer = await DownloadDataFromUrl(url);

                lastDownload = DateTime.UtcNow;

                if (uniqueKey != _uniqueKey)
                    return;

                using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
                {
                    stream.Seek(0, SeekOrigin.End);
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    await stream.FlushAsync();
                }

                InvokeProgressEvent((uint)slicesCount, (uint)i, FileTransferState.DataTransfer);

                message["_PartsWritten"] = i + 1;
                await ReceiveState.SaveState();
            }

            ReceiveState.Downloading.Remove(key);

            InvokeFinishedEvent((uint)slicesCount);
            await ReceiveSuccessful(serverIP, key);

            message["_State"] = "Finished";
            await ReceiveState.SaveState();
        }

        private static void InvokeFinishedEvent(uint currentFileSlicesCount)
        {
            if (!ReceiveState.IsQueue)
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = currentFileSlicesCount, Total = currentFileSlicesCount, State = FileTransferState.Finished, Guid = ReceiveState.RequestGuid, SenderName = ReceiveState.SenderName, TotalFiles = ReceiveState.FilesCount });
            else
                ReceiveState.QueueSlicesFinished += (int)currentFileSlicesCount;
        }

        private static void InvokeProgressEvent(uint currentFileSlicesCount, uint currentFileSlice, FileTransferState state)
        {
            if (state == FileTransferState.Finished)
                throw new InvalidOperationException();

            FileTransferProgressEventArgs ea = null;
            if (!ReceiveState.IsQueue)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = currentFileSlice + 1, Total = currentFileSlicesCount, State = FileTransferState.DataTransfer, Guid = ReceiveState.RequestGuid, SenderName = ReceiveState.SenderName, TotalFiles = ReceiveState.FilesCount };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }
            else if (state == FileTransferState.QueueList)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = 0, Total = 0, State = FileTransferState.QueueList, Guid = ReceiveState.RequestGuid, SenderName = ReceiveState.SenderName, TotalFiles = ReceiveState.FilesCount };
                System.Diagnostics.Debug.WriteLine("Downloading queue data...");
            }
            else
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = (ulong)(ReceiveState.QueueSlicesFinished + currentFileSlice + 1), Total = (ulong)ReceiveState.QueueTotalSlices, State = FileTransferState.QueueList, Guid = ReceiveState.RequestGuid, SenderName = ReceiveState.SenderName, TotalFiles = ReceiveState.FilesCount };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }

            FileTransferProgress?.Invoke(ea);
        }

        private static async Task<byte[]> DownloadDataFromUrl(string url)
        {
            int tryCount = 0;
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            while (true)
            {
                try
                {
                    tryCount++;

                    HttpClient client = new HttpClient()
                    {
                        Timeout = timeout,
                    };

                    Debug.WriteLine("Downloading " + url);
                    return await client.GetByteArrayAsync(url);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Download failed: {ex.Message}");

                    if (tryCount > 5)
                        throw;
                }

                timeout = timeout.Add(TimeSpan.FromSeconds(2));
            }
        }

        private static async Task<IFile> CreateFile(IFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
        }

        private static async Task<IFile> CreateOrOpenFile(IFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }

        private static async Task<IFolder> CreateDirectoryIfNecessary(IFolder downloadMainFolder, string directory)
        {
            string[] directories = directory.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (directories.Count() == 0)
                return downloadMainFolder;

            IFolder curFolder = downloadMainFolder;
            for (int i = 0; i < directories.Count(); i++)
            {
                curFolder = await curFolder.CreateFolderAsync(directories[i], CreationCollisionOption.OpenIfExists);
            }

            return curFolder;
        }

        private static async Task ReceiveFailed(string serverIP, string key, string message)
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync("http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/finish/?success=false&message=" + System.Net.WebUtility.UrlEncode(message));
            }
            catch { }
        }

        private static async Task ReceiveSuccessful(string serverIP, string key)
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync("http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/finish/?success=true");
            }
            catch { }
        }
    }
}
