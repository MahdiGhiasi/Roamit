using PCLStorage;
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

namespace QuickShare.FileTransfer
{
    public static class FileReceiver
    {
        static List<string> downloading = new List<string>();

        public delegate void ReceiveFileProgressEventHandler(FileTransferProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static bool isQueue = false;
        static ulong queueTotalSlices = 0;
        static uint queueSlicesFinished = 0;
        static uint queuedSlicesYet = 0;
        static List<Dictionary<string, object>> queueItems = null;
        static string queueFinishUrl = "";
        static int queueFilesCount = 0;

        static Guid requestGuid;
        static string senderName = "remote device";

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, IFolder downloadFolder)
        {
            Dictionary<string, object> returnVal = null;
            if ((request.ContainsKey("Type")) && (request["Type"] as string == "QueueInit"))
            {
                //Queue initialization

                isQueue = true;
                queueTotalSlices = (ulong)request["TotalSlices"];
                queueSlicesFinished = 0;
                queuedSlicesYet = 0;
                queueItems = new List<Dictionary<string, object>>();
                queueFinishUrl = "http://" + (request["ServerIP"] as string) + ":" + Constants.CommunicationPort + "/" + request["QueueFinishKey"] + "/finishQueue/";
                queueFilesCount = 0;

                returnVal = new Dictionary<string, object>();
                returnVal.Add("QueueInitialized", "1");

                requestGuid = (Guid)request["Guid"];
                senderName = (string)request["SenderName"];

                /* TODO: Do this from outside of this function */
                //await request.SendResponseAsync(vs);

                InvokeProgressEvent(0, 0, FileTransferState.QueueList);
            }
            else if (isQueue)
            {
                //Queue data details
                queuedSlicesYet += (uint)request["SlicesCount"];
                queueItems.Add(request);

                queueFilesCount++;

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
                await DownloadFile(request, downloadFolder);
            }

            return returnVal;
        }

        private static async Task BeginProcessingQueue(IFolder downloadFolder)
        {
            foreach (var item in queueItems)
            {
                await DownloadFile(item, downloadFolder);
            }

            FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = queueTotalSlices, Total = queueTotalSlices, State = FileTransferState.Finished, Guid = requestGuid, SenderName = senderName, TotalFiles = queueFilesCount });

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

            var slicesCount = (uint)message["SlicesCount"];
            var fileName = (string)message["FileName"];
            var dateModifiedMilliseconds = (long)message["DateModified"];
            var dateCreatedMilliseconds = (long)message["DateCreated"];
            var fileSize = (long)message["FileSize"];
            var directory = (string)message["Directory"];
            var serverIP = (string)message["ServerIP"];

            if (!isQueue)
            {
                requestGuid = (Guid)message["Guid"];
                senderName = (string)message["SenderName"];
            }


            var dateModified = DateTimeExtension.FromUnixTimeMilliseconds(dateModifiedMilliseconds);
            var dateCreated = DateTimeExtension.FromUnixTimeMilliseconds(dateCreatedMilliseconds);

            if (downloadMainFolder == null)
            {
                await ReceiveFailed(serverIP, key, "Default downloads folder hasn't been set.");
                return;
            }

            IFolder downloadFolder = await CreateDirectoryIfNecessary(downloadMainFolder, directory);

            IFile file = await CreateFile(downloadFolder, fileName);

            using (var stream = await file.OpenAsync(FileAccess.ReadAndWrite))
            {
                for (uint i = 0; i < slicesCount; i++)
                {
                    string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";

                    byte[] buffer = await DownloadDataFromUrl(url);

                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    InvokeProgressEvent(slicesCount, i, FileTransferState.DataTransfer);
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
                FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = currentFileSlicesCount, Total = currentFileSlicesCount, State = FileTransferState.Finished, Guid = requestGuid, SenderName = senderName, TotalFiles = queueFilesCount });
            else
                queueSlicesFinished += currentFileSlicesCount;
        }

        private static void InvokeProgressEvent(uint currentFileSlicesCount, uint currentFileSlice, FileTransferState state)
        {
            if (state == FileTransferState.Finished)
                throw new InvalidOperationException();

            FileTransferProgressEventArgs ea = null;
            if (!isQueue)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = currentFileSlice + 1, Total = currentFileSlicesCount, State = FileTransferState.DataTransfer, Guid = requestGuid, SenderName = senderName, TotalFiles = queueFilesCount };
                System.Diagnostics.Debug.WriteLine(ea.CurrentPart + " / " + ea.Total);
            }
            else if (state == FileTransferState.QueueList)
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = 0, Total = 0, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = queueFilesCount };
                System.Diagnostics.Debug.WriteLine("Downloading queue data...");
            }
            else
            {
                ea = new FileTransferProgressEventArgs { CurrentPart = queueSlicesFinished + currentFileSlice + 1, Total = queueTotalSlices, State = FileTransferState.QueueList, Guid = requestGuid, SenderName = senderName, TotalFiles = queueFilesCount };
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
