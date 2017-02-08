using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace QuickShare.FileSendReceive
{
    public static class FileReceiver
    {
        static List<string> downloading = new List<string>();

        public delegate void ReceiveFileProgressEventHandler(FileTransferProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static bool isQueue = false;
        static uint queueTotalSlices = 0;
        static uint queueSlicesFinished = 0;
        static uint queuedSlicesYet = 0;
        static List<ValueSet> queueItems = null;
        static string queueFinishUrl = "";

        public static async Task ReceiveRequest(AppServiceRequest request)
        {
            if ((request.Message.ContainsKey("Type")) && (request.Message["Type"] as string == "QueueInit"))
            {
                //Queue initialization

                isQueue = true;
                queueTotalSlices = (uint)request.Message["TotalSlices"];
                queueSlicesFinished = 0;
                queuedSlicesYet = 0;
                queueItems = new List<ValueSet>();
                queueFinishUrl = "http://" + (request.Message["ServerIP"] as string) + ":" + Constants.CommunicationPort + "/" + request.Message["QueueFinishKey"] + "/finishQueue/";

                ValueSet vs = new ValueSet();
                vs.Add("QueueInitialized", "1");
                await request.SendResponseAsync(vs);

                InvokeProgressEvent(0, 0, FileTransferState.QueueList);
            }
            else if (isQueue)
            {
                //Queue data details
                queuedSlicesYet += (uint)request.Message["SlicesCount"];
                queueItems.Add(request.Message);
                
                if (queuedSlicesYet == queueTotalSlices)
                    await BeginProcessingQueue();
                else if (queuedSlicesYet > queueTotalSlices)
                    throw new Exception("Queued more slices than expected.");
            }
            else
            {
                //Singular file
                await DownloadFile(request.Message);
            }
        }

        private static async Task BeginProcessingQueue()
        {
            foreach (var item in queueItems)
            {
                await DownloadFile(item);
            }

            FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = queueTotalSlices, Total = queueTotalSlices, State = FileTransferState.Finished });

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

        public static async Task DownloadFile(ValueSet message)
        {
            var key = (string)message["DownloadKey"];

            if (downloading.Contains(key))
                return;

            downloading.Add(key);

            Debug.WriteLine("Receive begin");

            var slicesCount = (uint)message["SlicesCount"];
            var fileName = (string)message["FileName"];
            var dateModifiedMilliseconds = (long)message["DateModified"];
            var dateCreatedMilliseconds = (long)message["DateCreated"];
            var fileSize = (ulong)message["FileSize"];
            var directory = (string)message["Directory"];
            var serverIP = (string)message["ServerIP"];

            var dateModified = DateTimeOffset.FromUnixTimeMilliseconds(dateModifiedMilliseconds).LocalDateTime;
            var dateCreated = DateTimeOffset.FromUnixTimeMilliseconds(dateCreatedMilliseconds).LocalDateTime;

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            StorageFolder downloadMainFolder;
            if (!futureAccessList.ContainsItem("downloadMainFolder"))
            {
                await ReceiveFailed(serverIP, key, "Default downloads folder hasn't been set.");
                return;
            }

            downloadMainFolder = await futureAccessList.GetItemAsync("downloadMainFolder") as StorageFolder;

            StorageFolder downloadFolder = await CreateDirectoryIfNecessary(downloadMainFolder, directory);

            StorageFile file = await CreateFile(downloadFolder, fileName);

            using (var stream = (await downloadFolder.OpenStreamForWriteAsync(file.Name, CreationCollisionOption.OpenIfExists)))
            {
                for (uint i = 0; i < slicesCount; i++)
                {
                    string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";

                    byte[] buffer = await DownloadDataFromUrl(url);

                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    InvokeProgressEvent(slicesCount, i, FileTransferState.DataTransfer);

                    System.Diagnostics.Debug.WriteLine((i + 1) + " / " + slicesCount);
                }

                await stream.FlushAsync();
            }

            //Debug.WriteLine(dateModified);
            //Debug.WriteLine(dateCreated);

            //System.IO.File.SetLastWriteTime(file.Path, dateModified);
            //System.IO.File.SetCreationTime(file.Path, dateCreated);

            downloading.Remove(key);

            await ReceiveSuccessful(serverIP, key);

            InvokeFinishedEvent(slicesCount);
        }

        private static void InvokeFinishedEvent(uint currentFileSlicesCount)
        {
            if (!isQueue)
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = currentFileSlicesCount, Total = currentFileSlicesCount, State = FileTransferState.Finished });
            else
                queueSlicesFinished += currentFileSlicesCount;
        }

        private static void InvokeProgressEvent(uint currentFileSlicesCount, uint currentFileSlice, FileTransferState state)
        {
            if (state == FileTransferState.Finished)
                throw new InvalidOperationException();

            if (!isQueue)
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = currentFileSlice + 1, Total = currentFileSlicesCount , State = FileTransferState.DataTransfer });
            else if (state == FileTransferState.QueueList)    
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = 0, Total = 0, State = FileTransferState.QueueList });
            else
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = queueSlicesFinished + currentFileSlicesCount, Total = queueTotalSlices, State = FileTransferState.QueueList });
        }

        private static async Task<byte[]> DownloadDataFromUrl(string url)
        {
            HttpClient client = new HttpClient();

            Debug.WriteLine("Downloading " + url);

            return await client.GetByteArrayAsync(url);
        }

        private static async Task<StorageFile> CreateFile(StorageFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
        }

        private static async Task<StorageFolder> CreateDirectoryIfNecessary(StorageFolder downloadMainFolder, string directory)
        {
            string[] directories = directory.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (directories.Count() == 0)
                return downloadMainFolder;

            StorageFolder curFolder = downloadMainFolder;
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
