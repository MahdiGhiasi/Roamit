using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using QuickShare.FileTransfer;
using QuickShare.Common.Rome;
using PCLStorage;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Threading;

namespace QuickShare.FileTransfer
{
    public class FileSender : IDisposable
    {
        public static readonly string TRANSFER_CANCELLED_MESSAGE = "Transfer cancelled.";
        public static readonly string FIRST_MESSAGE_TIMEOUT = "Can't reach remote device.";

        readonly int maxQueueInfoMessageSize = 1536;
        readonly TimeSpan handshakeTimeout = TimeSpan.FromSeconds(6);
        readonly TimeSpan prepareTimeout = TimeSpan.FromSeconds(6);

        object remoteSystem;

        ServerIPFinder ipFinder;

        TaskCompletionSource<bool> ipFinderTcs;
        IPDetectionCompletedEventArgs ipFinderResult = null;

        TaskCompletionSource<string> fileSendTcs;
        TaskCompletionSource<string> queueFinishTcs;

        List<string> myIPs;

        Dictionary<string, FileDetails> keyTable = new Dictionary<string, FileDetails>();

        IWebServer server;

        IWebServerGenerator webServerGenerator;
        IRomePackageManager packageManager;

        string deviceName;

        ulong bytesSent;

        public delegate void FileTransferProgressEventHandler(object sender, FileTransferProgressEventArgs e);
        public event FileTransferProgressEventHandler FileTransferProgress;
        private event FileTransferProgressEventHandler FileTransferProgressInternal;

        public FileSender(object _remoteSystem, IWebServerGenerator _webServerGenerator, IRomePackageManager _packageManager, IEnumerable<string> _myIPs, string _deviceName)
        {
            remoteSystem = _remoteSystem;
            webServerGenerator = _webServerGenerator;
            packageManager = _packageManager;

            ipFinder = new ServerIPFinder(webServerGenerator, packageManager);

            myIPs = new List<string>(_myIPs);

            deviceName = _deviceName;
        }

        public async Task<FileTransferResult> SendFiles(CancellationToken cancellationToken, IEnumerable<IFile> files, string directoryName)
        {
            List<Tuple<string, IFile>> l = new List<Tuple<string, IFile>>();
            foreach (var file in files)
            {
                l.Add(new Tuple<string, IFile>(directoryName, file));
            }

            return await SendQueue(cancellationToken, l, directoryName);
        }

        public async Task<FileTransferResult> SendFile(CancellationToken cancellationToken, IFile file, string directory = "", bool isQueue = false)
        {
            if ((ipFinderResult == null) || (ipFinderResult.Success == false))
            {
                await Handshake();

                if (ipFinderResult == null)
                    ipFinderResult = new IPDetectionCompletedEventArgs
                    {
                        Success = false,
                    };
            }

            if (ipFinderResult.Success == false)
                return FileTransferResult.FailedOnHandshake;

            bytesSent = 0;
            InitServer();

            var key = GenerateUniqueRandomKey();

            var properties = await file.GetFileStats();
            var slicesCount = (uint)Math.Ceiling(((double)properties.Length) / ((double)Constants.FileSliceMaxLength));

            keyTable.Add(key, new FileDetails
            {
                storageFile = file,
                lastPieceAccessed = 0,
                lastSliceSize = (uint)((ulong)properties.Length % Constants.FileSliceMaxLength),
                lastSliceId = slicesCount - 1
            });

            InitUrls(key, slicesCount);

            queueFinishTcs = null;
            fileSendTcs = new TaskCompletionSource<string>();

            ClearInternalEventSubscribers();
            FileTransferProgressInternal += (s, ee) =>
            {
                FileTransferProgress?.Invoke(s, ee);
            };
            
            cancellationToken.Register(() =>
            {
                fileSendTcs?.TrySetResult(TRANSFER_CANCELLED_MESSAGE);
                server?.Dispose();
            });

            //TODO: When fileSendTcs finishes but BeginSending is still not returned.
            
            //TODO: Also check SendQueue for similar thing

            if (!(await BeginSending(key, slicesCount, file.Name, properties, directory, false)))
                return FileTransferResult.FailedOnPrepare;

            return await WaitForFinish(cancellationToken);
        }

        private void ClearInternalEventSubscribers()
        {
            if (FileTransferProgressInternal == null)
                return;

            foreach (var d in FileTransferProgressInternal.GetInvocationList())
                FileTransferProgressInternal -= (d as FileTransferProgressEventHandler);
        }

        private string GenerateUniqueRandomKey(int length = 24)
        {
            string s = "";

            do
            {
                s = RandomFunctions.RandomString(length);
            }
            while (keyTable.ContainsKey(s));

            return s;
        }

        private async Task<FileTransferResult> WaitForFinish(CancellationToken cancellationToken)
        {
            TimeoutForTransferStart(fileSendTcs);

            var result = await fileSendTcs.Task;
            return ProcessTcsResult(result);
        }

        private async Task<FileTransferResult> WaitQueueToFinish(CancellationToken cancellationToken)
        {
            TimeoutForTransferStart(queueFinishTcs);

            var result = await queueFinishTcs.Task;
            return ProcessTcsResult(result);
        }

        private async void TimeoutForTransferStart(TaskCompletionSource<string> tcs)
        {
            await Task.Delay(prepareTimeout);

            if (bytesSent == 0)
            {
                tcs?.TrySetResult(FileSender.FIRST_MESSAGE_TIMEOUT);
            }
        }

        private static FileTransferResult ProcessTcsResult(string result)
        {
            if (result.Length != 0)
            {
                System.Diagnostics.Debug.WriteLine(result);

                if (result == TRANSFER_CANCELLED_MESSAGE)
                    return FileTransferResult.Cancelled;
                else if (result == FIRST_MESSAGE_TIMEOUT)
                    return FileTransferResult.FailedOnHandshake;
                else
                    return FileTransferResult.FailedOnSend;
            }

            return FileTransferResult.Successful;
        }

        /// <param name="files">A list of Tuple(Relative directory path, StorageFile) objects.</param>
        public async Task<FileTransferResult> SendQueue(CancellationToken cancellationToken, List<Tuple<string, IFile>> files, string parentDirectoryName)
        {
            if ((ipFinderResult == null) || (ipFinderResult.Success == false))
            {
                await Handshake();

                if (ipFinderResult == null)
                    ipFinderResult = new IPDetectionCompletedEventArgs
                    {
                        Success = false,
                    };
            }

            if (ipFinderResult.Success == false)
                return FileTransferResult.FailedOnHandshake;

            bytesSent = 0;
            InitServer();

            Dictionary<IFile, string> sFileKeyPairs = new Dictionary<IFile, string>();
            IFileStats[] fs = new IFileStats[files.Count];

            ulong totalSlices = 0;

            for (int i = 0; i < files.Count; i++)
            {
                var item = files[i];

                var key = GenerateUniqueRandomKey();

                fs[i] = await item.Item2.GetFileStats();
                var slicesCount = (uint)Math.Ceiling(((double)fs[i].Length) / ((double)Constants.FileSliceMaxLength));

                totalSlices += slicesCount;

                keyTable.Add(key, new FileDetails
                {
                    storageFile = item.Item2,
                    lastPieceAccessed = 0,
                    lastSliceSize = (uint)((ulong)fs[i].Length % Constants.FileSliceMaxLength),
                    lastSliceId = slicesCount - 1
                });

                sFileKeyPairs.Add(item.Item2, key);

                InitUrls(key, slicesCount);
            }

            var queueFinishKey = RandomFunctions.RandomString(15);

            server.AddResponseUrl("/" + queueFinishKey + "/finishQueue/", (Func<IWebServer, RequestDetails, string>)QueueFinished);
            System.Diagnostics.Debug.WriteLine("/" + queueFinishKey + "/finishQueue/");

            queueFinishTcs = new TaskCompletionSource<string>();
            fileSendTcs = null;

            ulong finishedSlices = 0;

            ClearInternalEventSubscribers();
            FileTransferProgressInternal += (s, ee) =>
            {
                FileTransferProgress?.Invoke(s, new FileTransferProgressEventArgs
                {
                    State = ee.State,
                    CurrentPart = finishedSlices + ee.CurrentPart,
                    Total = totalSlices,
                    TotalBytesTransferred = ee.TotalBytesTransferred,
                    TotalFiles = ee.TotalFiles,
                });

                if (ee.State == FileTransferState.Finished)
                    finishedSlices += ee.Total;
            };

            cancellationToken.Register(() =>
            {
                queueFinishTcs?.TrySetResult(TRANSFER_CANCELLED_MESSAGE);
                server?.Dispose();
            });

            var queueInfoKey = GenerateUniqueRandomKey(9);
            PrepareQueueInfo(queueInfoKey, files, sFileKeyPairs, fs);
            
            if (await SendQueueInit(totalSlices, queueFinishKey, parentDirectoryName, queueInfoKey) == false)
                return FileTransferResult.FailedOnQueueInit;

            bool infoSendResult = await WaitForQueueInfoToSend(files, sFileKeyPairs, fs);

            if (infoSendResult == false)
                return FileTransferResult.FailedOnSend;

            return await WaitQueueToFinish(cancellationToken);
        }

        string queueInfoComplete = "";
        bool sentQueueInfoComplete;
        private void PrepareQueueInfo(string key, List<Tuple<string, IFile>> files, Dictionary<IFile, string> sFileKeyPairs, IFileStats[] fs)
        {
            List<string> items = GetQueueInfoItems(files, sFileKeyPairs, fs).ToList();
            queueInfoComplete = JsonConvert.SerializeObject(items);
            sentQueueInfoComplete = false;

            server.AddResponseUrl($"/{key}/", (Func<IWebServer, RequestDetails, string>)GetQueueInfoComplete);
        }

        private string GetQueueInfoComplete(IWebServer arg1, RequestDetails arg2)
        {
            sentQueueInfoComplete = true;
            return queueInfoComplete;
        }

        private async Task<bool> WaitForQueueInfoToSend(List<Tuple<string, IFile>> files, Dictionary<IFile, string> sFileKeyPairs, IFileStats[] fs)
        {
            int c = 0;
            while (c < 18)
            {
                if (sentQueueInfoComplete)
                    return true;

                await Task.Delay(TimeSpan.FromSeconds(0.5));
                c++;
            }

            return sentQueueInfoComplete;
        }

        private Queue<string> GetQueueInfoItems(List<Tuple<string, IFile>> files, Dictionary<IFile, string> sFileKeyPairs, IFileStats[] fs)
        {
            Queue<string> items = new Queue<string>();
            for (int i = 0; i < files.Count; i++)
            {
                var key = sFileKeyPairs[files[i].Item2];
                items.Enqueue(GetQueueFileInfo(key,
                    keyTable[key].lastSliceId + 1,
                    files[i].Item2.Name,
                    fs[i],
                    files[i].Item1,
                    true));
            }

            return items;
        }

        private async Task<bool> SendQueueInfoParts(Queue<string> items)
        {
            int partNum = 0;
            while (items.Count > 0)
            {
                int totalSize = 0;
                List<string> curItems = new List<string>();

                while ((items.Count > 0) && ((totalSize + items.Peek().Length) <= maxQueueInfoMessageSize))
                {
                    string nextItem = items.Dequeue();
                    curItems.Add(nextItem);
                    totalSize += nextItem.Length;
                }

                string data = JsonConvert.SerializeObject(curItems);

                Debug.WriteLine($"QueueItemGroup #{partNum} contains {curItems.Count} items. Sending...");

                Dictionary<string, object> qData = new Dictionary<string, object>
                {
                    { "IsQueueItemGroup", "true" },
                    { "PartNum", partNum },
                    { "Data", data },
                    { "Receiver", "FileReceiver" },
                };

                int tryCount = 0;
                RomeAppServiceResponse result;
                do
                {
                    result = await packageManager.Send(qData);
                    tryCount++;
                }
                while ((result.Status != RomeAppServiceResponseStatus.Success) && (tryCount < 2));

                if (result.Status != RomeAppServiceResponseStatus.Success)
                    return false;

                Debug.WriteLine("Sent.");
                partNum++;
            }

            return true;
        }

        private async Task<bool> SendQueueInit(ulong totalSlices, string queueFinishKey, string parentDirectoryName, string queueInfoKey)
        {
            Dictionary<string, object> qInit = new Dictionary<string, object>
            {
                { "Receiver", "FileReceiver" },
                { "Type", "QueueInit" },
                { "TotalSlices", (long)totalSlices },
                { "QueueFinishKey", queueFinishKey },
                { "ServerIP", ipFinderResult.MyIP },
                { "Guid", Guid.NewGuid().ToString() },
                { "SenderName", deviceName },
                { "parentDirectoryName", parentDirectoryName },
                { "QueueInfoKey", queueInfoKey },
            };
            var result = await packageManager.Send(qInit);

            if (result.Status == RomeAppServiceResponseStatus.Success)
                return true;
            else
            {
                System.Diagnostics.Debug.WriteLine("SendQueueInit: Send failed (" + result.Status.ToString() + ")");
                return false;
            }
        }

        private string GetQueueFileInfo(string key, uint slicesCount, string fileName, IFileStats properties, string directory, bool isQueue)
        {
            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "FileReceiver" },
                { "DownloadKey", key },
                { "SlicesCount", (int)slicesCount },
                { "FileName", fileName },
                { "DateModified", properties.LastWriteTime.ToUnixTimeMilliseconds() },
                { "DateCreated", properties.CreationTime.ToUnixTimeMilliseconds() },
                { "FileSize", properties.Length },
                { "Directory", directory },
                { "ServerIP", ipFinderResult.MyIP }
            };
            var serialized = JsonConvert.SerializeObject(vs, Formatting.None);
            return serialized;
        }

        private async Task<bool> BeginSending(string key, uint slicesCount, string fileName, IFileStats properties, string directory, bool isQueue)
        {
            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "FileReceiver" },
                { "DownloadKey", key },
                { "SlicesCount", (int)slicesCount },
                { "FileName", fileName },
                { "DateModified", properties.LastWriteTime.ToUnixTimeMilliseconds() },
                { "DateCreated", properties.CreationTime.ToUnixTimeMilliseconds() },
                { "FileSize", properties.Length },
                { "Directory", directory },
                { "ServerIP", ipFinderResult.MyIP }
            };
            if (!isQueue)
            {
                vs.Add("Guid", Guid.NewGuid().ToString());
                vs.Add("SenderName", deviceName);
            }
            else
            {
                vs.Add("IsQueueItem", "true");
            }

            var result = await packageManager.Send(vs);

            if (result.Status == RomeAppServiceResponseStatus.Success)
            {
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BeginSending: Send failed (" + result.Status.ToString() + ")");
                return false;
            }
        }

        private void InitUrls(string key, uint slicesCount)
        {
            for (int i = 0; i < slicesCount; i++)
            {
                server.AddResponseUrl("/" + key + "/" + i.ToString() + "/", (Func<IWebServer, RequestDetails, Task<byte[]>>)GetFileSlice);
                System.Diagnostics.Debug.WriteLine("/" + key + "/" + i.ToString() + "/");
            }

            server.AddResponseUrl("/" + key + "/finish/", (Func<IWebServer, RequestDetails, string>)SendFinished);
            System.Diagnostics.Debug.WriteLine("/" + key + "/finish/");
        }

        private string SendFinished(IWebServer sender, RequestDetails request)
        {
            try
            {
                var query = QueryHelpers.ParseQuery(request.Url.Query);

                var success = (query["success"][0].ToLower() == "true");
                var message = "";

                string[] parts = request.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];

                if (success)
                    FileTransferProgressInternal?.Invoke(this, new FileTransferProgressEventArgs { CurrentPart = keyTable[key].lastSliceId + 1, Total = keyTable[key].lastSliceId + 1, State = FileTransferState.Finished });
                else
                {
                    message = query["message"][0];
                    FileTransferProgressInternal?.Invoke(this, new FileTransferProgressEventArgs { CurrentPart = keyTable[key].lastSliceId + 1, Total = keyTable[key].lastSliceId + 1, State = FileTransferState.Error, Message = message });
                }

                fileSendTcs?.TrySetResult(message);
            }
            catch (Exception ex)
            {
                fileSendTcs?.TrySetResult(ex.Message);
            }

            return "OK";
        }

        private string QueueFinished(IWebServer sender, RequestDetails request)
        {
            try
            {
                var query = QueryHelpers.ParseQuery(request.Url.Query);

                var success = (query["success"][0].ToLower() == "true");
                var message = "";

                if (!success)
                    message = query["message"][0];

                queueFinishTcs.TrySetResult(message);
            }
            catch (Exception ex)
            {
                queueFinishTcs.TrySetResult(ex.Message);
            }

            return "OK";
        }


        private async Task<byte[]> GetFileSlice(IWebServer sender, RequestDetails request)
        {
            try
            {
                string[] parts = request.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];
                ulong id = ulong.Parse(parts[1]);

                IFile file = keyTable[key].storageFile;
                int pieceSize = ((keyTable[key].lastSliceId != id) || (keyTable[key].lastSliceSize == 0)) ? (int)Constants.FileSliceMaxLength : (int)keyTable[key].lastSliceSize;

                if (id >= keyTable[key].lastPieceAccessed)
                {
                    bytesSent += (ulong)pieceSize;
                    FileTransferProgressInternal?.Invoke(this, new FileTransferProgressEventArgs { CurrentPart = id + 1, Total = keyTable[key].lastSliceId + 1, State = FileTransferState.DataTransfer, TotalBytesTransferred = bytesSent });
                    keyTable[key].lastPieceAccessed = (uint)id;
                }

                byte[] buffer = new byte[pieceSize];

                using (Stream stream = await file.OpenAsync(PCLStorage.FileAccess.Read))
                {
                    stream.Seek((int)(id * Constants.FileSliceMaxLength), SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, pieceSize);
                }

                return buffer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in GetFileSlice(): " + ex.Message);
                return "Invalid Request".Select(c => (byte)c).ToArray();
            }
        }

        private void InitServer()
        {
            if (server != null)
                server.Dispose();

            server = webServerGenerator.GenerateInstance();
            server.StartWebServer(ipFinderResult.MyIP, Constants.CommunicationPort);
        }

        private async Task<bool> Handshake()
        {
            if (myIPs.Count == 1)
            {
                Debug.WriteLine("Device has only one IP, so IPFinder will not be called.");
                ipFinderResult = new IPDetectionCompletedEventArgs
                {
                    MyIP = myIPs[0],
                    Success = true,
                };
                return true;
            }

            ipFinder.IPDetectionCompleted += IpFinder_IPDetectionCompleted;
            ipFinderTcs = new TaskCompletionSource<bool>();
            if (await ipFinder.StartFindingMyLocalIP(myIPs))
            {
                await ipFinderTcs.Task.WithTimeout(handshakeTimeout);

                if (ipFinderResult != null)
                {
                    System.Diagnostics.Debug.WriteLine(ipFinderResult.MyIP);
                    return true;
                }
            }

            Debug.WriteLine("Sending handshake message failed.");
            ipFinderResult = new IPDetectionCompletedEventArgs
            {
                Success = false,
            };
            return false;
        }

        private void IpFinder_IPDetectionCompleted(object sender, IPDetectionCompletedEventArgs e)
        {
            Debug.WriteLine("IpFinder_IPDetectionCompleted.");
            ipFinderResult = e;
            ipFinderTcs.SetResult(true);
        }

        public void Dispose()
        {
            ipFinder.Dispose();
            if (server != null)
                server.Dispose();
        }
    }

    public enum FileTransferResult
    {
        Successful = 1,
        FailedOnHandshake = 2,
        FailedOnQueueInit = 3,
        FailedOnPrepare = 4,
        FailedOnSend = 5,
        Cancelled = 6,
        NoFiles = 7,
        Timeout = 8,
    }
}
