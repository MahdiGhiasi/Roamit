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
        static List<string> downloading = new List<string>();

        public delegate void ReceiveFileProgressEventHandler(FileTransferProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static bool isQueue = false;
        static long queueTotalSlices = 0;
        static int queueSlicesFinished = 0;
        static int queuedSlicesYet = 0;
        static List<Dictionary<string, object>> queueItems = null;
        static string queueFinishUrl = "";
        static int filesCount = 0;
        static string queueParentDirectory = "";

        static int latestReceivedQueueItemGroupId = -1;

        static Guid requestGuid;
        static string senderName = "remote device";

        static ulong totalBytesReceived = 0;

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            Dictionary<string, object> returnVal = null;

            if ((request.ContainsKey("Type")) && (request["Type"] as string == "QueueInit"))
            {
                //Queue initialization

                isQueue = true;
                queueTotalSlices = (long)request["TotalSlices"];
                queueSlicesFinished = 0;
                queuedSlicesYet = 0;
                queueItems = new List<Dictionary<string, object>>();
                queueFinishUrl = "http://" + (request["ServerIP"] as string) + ":" + Constants.CommunicationPort + "/" + request["QueueFinishKey"] + "/finishQueue/";
                filesCount = 0;
                queueParentDirectory = (string)request["parentDirectoryName"];
                latestReceivedQueueItemGroupId = -1;
                totalBytesReceived = 0;

                returnVal = new Dictionary<string, object>
                {
                    { "QueueInitialized", "1" }
                };
                requestGuid = Guid.Parse(request["Guid"] as string);
                senderName = (string)request["SenderName"];

                /* TODO: Do this from outside of this function */
                //await request.SendResponseAsync(vs);

                InvokeProgressEvent(0, 0, FileTransferState.QueueList);
            }
            else if ((request.ContainsKey("IsQueueItemGroup")) && (request["IsQueueItemGroup"] as string == "true"))
            {
                int partNum = int.Parse(request["PartNum"].ToString());
                Debug.WriteLine($"Received QueueItemGroup #{partNum}.");
                if (partNum > latestReceivedQueueItemGroupId)
                {
                    latestReceivedQueueItemGroupId = partNum;

                    var items = JsonConvert.DeserializeObject<List<string>>(request["Data"].ToString());

                    Debug.WriteLine($"It contains {items.Count} entries. Processing...");

                    foreach (var item in items)
                    {
                        var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(item);
                        await ProcessQueueItem(info, downloadFolderDecider);
                    }

                    Debug.WriteLine("Processed QueueItemGroup successfully.");
                }
            }
            else if ((request.ContainsKey("IsQueueItem")) && (request["IsQueueItem"] as string == "true"))
            {
                await ProcessQueueItem(request, downloadFolderDecider);
            }
            else
            {
                //Singular file
                filesCount = 1;
                requestGuid = Guid.Parse(request["Guid"] as string);
                senderName = (string)request["SenderName"];
                isQueue = false;
                totalBytesReceived = 0;

                var downloadFolder = await downloadFolderDecider(new string[] { Path.GetExtension((string)request["FileName"]) });
                
                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.Add(requestGuid,
                    DateTime.Now,
                    senderName,
                    new ReceivedFileCollection
                    {
                        Files = new List<ReceivedFile>()
                        {
                            new ReceivedFile
                            {
                                Name = (string)request["FileName"],
                                Size = (long)request["FileSize"],
                                StorePath = System.IO.Path.Combine(downloadFolder.Path, (string)request["Directory"]),
                            }
                        },
                        StoreRootPath = System.IO.Path.Combine(downloadFolder.Path, (string)request["Directory"]),
                    },
                    false, false);
                DataStorageProviders.HistoryManager.Close();

                await DownloadFile(request, downloadFolder);

                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.ChangeCompletedStatus(requestGuid, true);
                DataStorageProviders.HistoryManager.Close();
            }

            return returnVal;
        }

        private static async Task ProcessQueueItem(Dictionary<string, object> request, Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            //Queue data details
            queuedSlicesYet += (int)(long)request["SlicesCount"];
            queueItems.Add(request);

            filesCount++;

            if (queuedSlicesYet == queueTotalSlices)
                await BeginProcessingQueue(downloadFolderDecider);
            else if (queuedSlicesYet > queueTotalSlices)
            {
                Debug.WriteLine("Queued more slices than expected.");
                throw new Exception("Queued more slices than expected.");
            }
        }

        public static void ClearEventRegistrations()
        {
            FileTransferProgress = null;
        }

        private static async Task BeginProcessingQueue(Func<string[], Task<IFolder>> downloadFolderDecider)
        {
            var downloadFolder = await downloadFolderDecider(queueItems.Select(x => Path.GetExtension((string)x["FileName"])).ToArray());

            string queueParentDirectory2 = await GetUniqueQueueParentDirectory(downloadFolder);

            foreach (var item in queueItems)
            {
                if ((((string)item["Directory"]).Length >= queueParentDirectory.Length) && (((string)item["Directory"]).Substring(0, queueParentDirectory.Length) == queueParentDirectory))
                {
                    if (queueParentDirectory2 != queueParentDirectory)
                        item["Directory"] = queueParentDirectory2 + ((string)item["Directory"]).Substring(queueParentDirectory.Length);
                }
                else if (queueParentDirectory.Length > 0)
                {
                    item["Directory"] = Path.Combine(queueParentDirectory2, (string)item["Directory"]);
                }
            }

            var logItems = from x in queueItems
                           select new ReceivedFile
                           {
                               Name = (string)x["FileName"],
                               Size = (long)x["FileSize"],
                               StorePath = System.IO.Path.Combine(downloadFolder.Path, (string)x["Directory"]),
                           };

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Add(requestGuid,
                DateTime.Now,
                senderName,
                new ReceivedFileCollection
                {
                    Files = logItems.ToList(),
                    StoreRootPath = System.IO.Path.Combine(downloadFolder.Path, queueParentDirectory2),
                },
                false);
            DataStorageProviders.HistoryManager.Close();

            foreach (var item in queueItems)
            {
                await DownloadFile(item, downloadFolder);
            }

            FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = (ulong)queueTotalSlices, Total = (ulong)queueTotalSlices, State = FileTransferState.Finished, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount });

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.ChangeCompletedStatus(requestGuid, true);
            DataStorageProviders.HistoryManager.Close();

            await QueueProcessFinishedNotifySender();
        }

        private static async Task<string> GetUniqueQueueParentDirectory(IFolder downloadFolder)
        {
            var queueParentDirectory2 = queueParentDirectory;

            if (queueParentDirectory.Length > 0)
            {
                var storeRootIndex = 2;
                while (await downloadFolder.CheckExistsAsync(queueParentDirectory2) == ExistenceCheckResult.FolderExists)
                {
                    queueParentDirectory2 = $"{queueParentDirectory} ({storeRootIndex})";
                    storeRootIndex++;
                }
            }

            return queueParentDirectory2;
        }

        private static async Task QueueProcessFinishedNotifySender()
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync(queueFinishUrl + "?success=true");
            }
            catch { }
        }

        public static async Task DownloadFile(Dictionary<string, object> message, IFolder downloadMainFolder)
        {
            var key = (string)message["DownloadKey"];

            if (downloading.Contains(key))
                return;

            downloading.Add(key);

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

            IFile file = await CreateFile(downloadFolder, fileName);

            if (file.Name != fileName) //File already existed, so new name generated for it. We should update database now.
            {
                await DataStorageProviders.HistoryManager.OpenAsync();
                DataStorageProviders.HistoryManager.UpdateFileName(requestGuid, fileName, file.Name, downloadFolder.Path);
                DataStorageProviders.HistoryManager.Close();

                await DataStorageProviders.HistoryManager.OpenAsync();
                var x = DataStorageProviders.HistoryManager.GetItem(requestGuid);
                var y = x.Data as ReceivedFileCollection;
                var z = y.Files[0].Name;
                var t = x.Id;
                DataStorageProviders.HistoryManager.Close();
            }

            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                for (uint i = 0; i < slicesCount; i++)
                {
                    string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";

                    byte[] buffer = await DownloadDataFromUrl(url);

                    int expectedLength;
                    if (i == (slicesCount - 1))
                        expectedLength = (int)(fileSize % ((long)Constants.FileSliceMaxLength));
                    else
                        expectedLength = (int)Constants.FileSliceMaxLength;

                    if (buffer.Length != expectedLength)
                    {
                        Debug.WriteLine("Slice length violation! Will retry...");
                        i--;
                        continue;
                    }

                    totalBytesReceived += (ulong)expectedLength;

                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    InvokeProgressEvent((uint)slicesCount, i, FileTransferState.DataTransfer);
                }

                await stream.FlushAsync();
            }

            //Debug.WriteLine(dateModified);
            //Debug.WriteLine(dateCreated);

            //System.IO.File.SetLastWriteTime(file.Path, dateModified);
            //System.IO.File.SetCreationTime(file.Path, dateCreated);

            downloading.Remove(key);

            InvokeFinishedEvent((uint)slicesCount);
            await ReceiveSuccessful(serverIP, key);
        }

        private static void InvokeFinishedEvent(uint currentFileSlicesCount)
        {
            if (!isQueue)
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = currentFileSlicesCount, Total = currentFileSlicesCount, State = FileTransferState.Finished, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount });
            else
                queueSlicesFinished += (int)currentFileSlicesCount;
        }

        private static void InvokeProgressEvent(uint currentFileSlicesCount, uint currentFileSlice, FileTransferState state)
        {
            if (state == FileTransferState.Finished)
                throw new InvalidOperationException();

            FileTransferProgressEventArgs ea = null;
            if (!isQueue)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = currentFileSlice + 1, Total = currentFileSlicesCount, State = FileTransferState.DataTransfer, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount, TotalBytesTransferred = totalBytesReceived };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }
            else if (state == FileTransferState.QueueList)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = 0, Total = 0, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount, TotalBytesTransferred = totalBytesReceived };
                System.Diagnostics.Debug.WriteLine("Downloading queue data...");
            }
            else
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = (ulong)(queueSlicesFinished + currentFileSlice + 1), Total = (ulong)queueTotalSlices, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount, TotalBytesTransferred = totalBytesReceived };
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
                catch
                {
                    Debug.WriteLine("Failed.");

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
