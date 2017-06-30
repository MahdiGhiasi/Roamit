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

namespace QuickShare.FileTransfer
{
    public class FileSender : IDisposable
    {
        readonly TimeSpan handshakeTimeout = TimeSpan.FromSeconds(10);

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

        public async Task<FileTransferResult> SendFiles(IEnumerable<IFile> files, string directoryName)
        {
            List<Tuple<string, IFile>> l = new List<Tuple<string, IFile>>();
            foreach (var file in files)
            {
                l.Add(new Tuple<string, IFile>(directoryName, file));
            }

            return await SendQueue(l, directoryName);
        }

        public async Task<FileTransferResult> SendFile(IFile file, string directory = "", bool isQueue = false)
        {
            if ((ipFinderResult == null) || (ipFinderResult.Success == false))
            {
                await Handshake().WithTimeout(handshakeTimeout);

                if (ipFinderResult == null)
                    ipFinderResult = new IPDetectionCompletedEventArgs
                    {
                        Success = false,
                    };
            }

            if (ipFinderResult.Success == false)
                return FileTransferResult.FailedOnHandshake;

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

            fileSendTcs = new TaskCompletionSource<string>();

            ClearInternalEventSubscribers();
            FileTransferProgressInternal += (s, ee) =>
            {
                FileTransferProgress?.Invoke(s, ee);
            };

            if (!(await BeginSending(key, slicesCount, file.Name, properties, directory, false)))
                return FileTransferResult.FailedOnPrepare;

            if (!(await WaitForFinish()))
                return FileTransferResult.FailedOnSend;

            return FileTransferResult.Successful;
        }

        private void ClearInternalEventSubscribers()
        {
            if (FileTransferProgressInternal == null)
                return;

            foreach (var d in FileTransferProgressInternal.GetInvocationList())
                FileTransferProgressInternal -= (d as FileTransferProgressEventHandler);
        }

        private string GenerateUniqueRandomKey()
        {
            string s = "";

            do
            {
                s = RandomFunctions.RandomString(24);
            }
            while (keyTable.ContainsKey(s));

            return s;
        }

        private async Task<bool> WaitForFinish()
        {
            var result = await fileSendTcs.Task;

            if (result.Length != 0)
            {
                System.Diagnostics.Debug.WriteLine(result);
                return false;
            }

            return true;
        }

        private async Task<bool> WaitQueueToFinish()
        {
            var result = await queueFinishTcs.Task;

            if (result.Length != 0)
            {
                System.Diagnostics.Debug.WriteLine(result);
                return false;
            }

            return true;
        }

        /// <param name="files">A list of Tuple(Relative directory path, StorageFile) objects.</param>
        private async Task<FileTransferResult> SendQueue(List<Tuple<string, IFile>> files, string parentDirectoryName)
        {
            if ((ipFinderResult == null) || (ipFinderResult.Success == false))
            {
                await Handshake().WithTimeout(handshakeTimeout);

                if (ipFinderResult == null)
                    ipFinderResult = new IPDetectionCompletedEventArgs
                    {
                        Success = false,
                    };
            }

            if (ipFinderResult.Success == false)
                return FileTransferResult.FailedOnHandshake;


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
                    Total = totalSlices
                });

                if (ee.State == FileTransferState.Finished)
                    finishedSlices += ee.Total;
            };

            if (await SendQueueInit(totalSlices, queueFinishKey, parentDirectoryName) == false)
                return FileTransferResult.FailedOnQueueInit;

            for (int i = 0; i < files.Count; i++)
            {
                var key = sFileKeyPairs[files[i].Item2];
                if (!(await BeginSending(key,
                                         keyTable[key].lastSliceId + 1,
                                         files[i].Item2.Name,
                                         fs[i],
                                         files[i].Item1,
                                         true)))
                    return FileTransferResult.FailedOnPrepare;
            }

            if (!(await WaitQueueToFinish()))
                return FileTransferResult.FailedOnSend;

            return FileTransferResult.Successful;
        }

        public async Task<FileTransferResult> SendFolder(IFolder folder, string parentDirectoryName)
        {
            List<Tuple<string, IFile>> files = await GetFiles(folder);

            return await SendQueue(files, parentDirectoryName);
        }

        private async Task<List<Tuple<string, IFile>>> GetFiles(IFolder f, string relPath = "")
        {
            List<Tuple<string, IFile>> files = (from x in await f.GetFilesAsync()
                                                select new Tuple<string, IFile>(relPath + f.Name + "\\", x)).ToList();

            var folders = await f.GetFoldersAsync();

            foreach (var folder in folders)
            {
                files.AddRange(await GetFiles(folder, relPath + f.Name + "\\"));
            }

            return files;
        }

        private async Task<bool> SendQueueInit(ulong totalSlices, string queueFinishKey, string parentDirectoryName)
        {
            Dictionary<string, object> qInit = new Dictionary<string, object>();
            qInit.Add("Receiver", "FileReceiver");
            qInit.Add("Type", "QueueInit");
            qInit.Add("TotalSlices", (long)totalSlices);
            qInit.Add("QueueFinishKey", queueFinishKey);
            qInit.Add("ServerIP", ipFinderResult.MyIP);
            qInit.Add("Guid", Guid.NewGuid().ToString());
            qInit.Add("SenderName", deviceName);
            qInit.Add("parentDirectoryName", parentDirectoryName);

            var result = await packageManager.Send(qInit);

            if (result.Status == RomeAppServiceResponseStatus.Success)
                return true;
            else
            {
                System.Diagnostics.Debug.WriteLine("SendQueueInit: Send failed (" + result.Status.ToString() + ")");
                return false;
            }
        }

        private async Task<bool> BeginSending(string key, uint slicesCount, string fileName, IFileStats properties, string directory, bool isQueue)
        {
            Dictionary<string, object> vs = new Dictionary<string, object>();
            vs.Add("Receiver", "FileReceiver");
            vs.Add("DownloadKey", key);
            vs.Add("SlicesCount", (int)slicesCount);
            vs.Add("FileName", fileName);
            vs.Add("DateModified", properties.LastWriteTime.ToUnixTimeMilliseconds());
            vs.Add("DateCreated", properties.CreationTime.ToUnixTimeMilliseconds());
            vs.Add("FileSize", properties.Length);
            vs.Add("Directory", directory);
            vs.Add("ServerIP", ipFinderResult.MyIP);

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

                fileSendTcs?.SetResult(message);
            }
            catch (Exception ex)
            {
                fileSendTcs?.SetResult(ex.Message);
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

                queueFinishTcs.SetResult(message);
            }
            catch (Exception ex)
            {
                queueFinishTcs.SetResult(ex.Message);
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

                if (id >= keyTable[key].lastPieceAccessed)
                {
                    FileTransferProgressInternal?.Invoke(this, new FileTransferProgressEventArgs { CurrentPart = id + 1, Total = keyTable[key].lastSliceId + 1, State = FileTransferState.DataTransfer });
                    keyTable[key].lastPieceAccessed = (uint)id;
                }

                IFile file = keyTable[key].storageFile;

                int pieceSize = ((keyTable[key].lastSliceId != id) || (keyTable[key].lastSliceSize == 0)) ? (int)Constants.FileSliceMaxLength : (int)keyTable[key].lastSliceSize;

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

        private async Task Handshake()
        {
            ipFinder.IPDetectionCompleted += IpFinder_IPDetectionCompleted;
            ipFinderTcs = new TaskCompletionSource<bool>();
            await ipFinder.StartFindingMyLocalIP(myIPs);
            await ipFinderTcs.Task;
            System.Diagnostics.Debug.WriteLine(ipFinderResult.MyIP);
        }

        private void IpFinder_IPDetectionCompleted(object sender, IPDetectionCompletedEventArgs e)
        {
            ipFinderResult = e;
            ipFinderTcs.SetResult(true);
        }

        public void Dispose()
        {
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
    }
}
