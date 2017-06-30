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

        static Guid requestGuid;
        static string senderName = "remote device";

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, IFolder downloadFolder)
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

                returnVal = new Dictionary<string, object>();
                returnVal.Add("QueueInitialized", "1");

                requestGuid = Guid.Parse(request["Guid"] as string);
                senderName = (string)request["SenderName"];

                /* TODO: Do this from outside of this function */
                //await request.SendResponseAsync(vs);

                InvokeProgressEvent(0, 0, FileTransferState.QueueList);
            }
            else if ((request.ContainsKey("IsQueueItem")) && (request["IsQueueItem"] as string == "true"))
            {
                //Queue data details
                queuedSlicesYet += (int)request["SlicesCount"];
                queueItems.Add(request);

                filesCount++;

                if (queuedSlicesYet == queueTotalSlices)
                    await BeginProcessingQueue(downloadFolder);
                else if (queuedSlicesYet > queueTotalSlices)
                {
                    Debug.WriteLine("Queued more slices than expected.");
                    throw new Exception("Queued more slices than expected.");
                }
            }
            else
            {
                //Singular file
                filesCount = 1;
                requestGuid = Guid.Parse(request["Guid"] as string);
                senderName = (string)request["SenderName"];
                isQueue = false;

                DataStorageProviders.HistoryManager.Open();
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
                    false);
                DataStorageProviders.HistoryManager.Close();

                await DownloadFile(request, downloadFolder);

                DataStorageProviders.HistoryManager.Open();
                DataStorageProviders.HistoryManager.ChangeCompletedStatus(requestGuid, true);
                DataStorageProviders.HistoryManager.Close();
            }

            return returnVal;
        }

        public static void ClearEventRegistrations()
        {
            FileTransferProgress = null;
        }

        private static async Task BeginProcessingQueue(IFolder downloadFolder)
        {
            var logItems = from x in queueItems
                           select new ReceivedFile
                           {
                               Name = (string)x["FileName"],
                               Size = (long)x["FileSize"],
                               StorePath = System.IO.Path.Combine(downloadFolder.Path, (string)x["Directory"]),
                           };

            DataStorageProviders.HistoryManager.Open();
            DataStorageProviders.HistoryManager.Add(requestGuid,
                DateTime.Now,
                senderName,
                new ReceivedFileCollection
                {
                    Files = logItems.ToList(),
                    StoreRootPath = System.IO.Path.Combine(downloadFolder.Path, queueParentDirectory),
                },
                false);
            DataStorageProviders.HistoryManager.Close();

            foreach (var item in queueItems)
            {
                await DownloadFile(item, downloadFolder);
            }

            FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = (ulong)queueTotalSlices, Total = (ulong)queueTotalSlices, State = FileTransferState.Finished, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount });

            DataStorageProviders.HistoryManager.Open();
            DataStorageProviders.HistoryManager.ChangeCompletedStatus(requestGuid, true);
            DataStorageProviders.HistoryManager.Close();

            await QueueProcessFinishedNotifySender();
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

            var slicesCount = (int)message["SlicesCount"];
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
                DataStorageProviders.HistoryManager.Open();
                DataStorageProviders.HistoryManager.UpdateFileName(requestGuid, fileName, file.Name, System.IO.Path.Combine(downloadFolder.Path, directory));
                DataStorageProviders.HistoryManager.Close();
            }

            using (var stream = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            {
                for (uint i = 0; i < slicesCount; i++)
                {
                    string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";

                    byte[] buffer = await DownloadDataFromUrl(url);

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
                ea = new FileTransferProgressEventArgs { CurrentPart = currentFileSlice + 1, Total = currentFileSlicesCount, State = FileTransferState.DataTransfer, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }
            else if (state == FileTransferState.QueueList)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = 0, Total = 0, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount };
                System.Diagnostics.Debug.WriteLine("Downloading queue data...");
            }
            else
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = (ulong)(queueSlicesFinished + currentFileSlice + 1), Total = (ulong)queueTotalSlices, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = filesCount };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }

            FileTransferProgress?.Invoke(ea);
        }

        private static async Task<byte[]> DownloadDataFromUrl(string url)
        {
            HttpClient client = new HttpClient();

            Debug.WriteLine("Downloading " + url);

            return await client.GetByteArrayAsync(url);
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
