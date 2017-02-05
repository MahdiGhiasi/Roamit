using QuickShare.Common;
using QuickShare.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.RemoteSystems;
using Windows.Storage.FileProperties;
using Windows.Foundation;

namespace QuickShare.FileSendReceive
{
    public class FileSender : IDisposable
    {
        RemoteSystem remoteSystem;

        ServerIPFinder ipFinder = new ServerIPFinder();

        TaskCompletionSource<bool> ipFinderTcs;
        IPDetectionCompletedEventArgs ipFinderResult = null;

        TaskCompletionSource<string> fileSendTcs;

        Dictionary<string, FileDetails> keyTable = new Dictionary<string, FileDetails>();

        WebServer server;

        public delegate void FileTransferProgressEventHandler(object sender, FileTransferProgressEventArgs e);
        public event FileTransferProgressEventHandler FileTransferProgress;

        public FileSender(RemoteSystem rs)
        {
            remoteSystem = rs;
        }

        public async Task<bool> SendFile(StorageFile file, string directory = "")
        {
            if ((ipFinderResult == null) || (ipFinderResult.Success == false))
                await Handshake();

            if (ipFinderResult.Success == false)
                return false;

            InitServer();

            var key = RandomFunctions.RandomString(16);
            
            var properties = await file.GetBasicPropertiesAsync();
            var slicesCount = (uint)Math.Ceiling(((double)properties.Size) / ((double)Constants.FileSliceMaxLength));

            keyTable.Add(key, new FileDetails
            {
                storageFile = file,
                lastPieceAccessed = 0,
                lastSliceSize = (uint)(properties.Size % Constants.FileSliceMaxLength),
                lastSliceId = slicesCount - 1
            });

            InitUrls(key, slicesCount);

            fileSendTcs = new TaskCompletionSource<string>();

            if (!(await BeginSending(key, slicesCount, file.Name, properties, file.DateCreated, directory)))
                return false;

            if (!(await WaitForFinish()))
                return false;

            return true;
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

        private async Task<bool> BeginSending(string key, uint slicesCount, string fileName, BasicProperties properties, DateTimeOffset dateCreated, string directory)
        {
            ValueSet vs = new ValueSet();
            vs.Add("Receiver", "FileReceiver");
            vs.Add("DownloadKey", key);
            vs.Add("SlicesCount", slicesCount);
            vs.Add("FileName", fileName);
            vs.Add("DateModified", properties.DateModified.ToUnixTimeMilliseconds());
            vs.Add("DateCreated", dateCreated.ToUnixTimeMilliseconds());
            vs.Add("FileSize", properties.Size);
            vs.Add("Directory", directory);
            vs.Add("ServerIP", ipFinderResult.MyIP);

            var result = await Rome.RomePackageManager.Instance.Send(vs);

            if (result.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
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
                server.AddResponseUrl("/" + key + "/" + i.ToString() + "/", (Func<WebServer, HttpListenerRequest, Task<byte[]>>)GetFileSlice);
                System.Diagnostics.Debug.WriteLine("/" + key + "/" + i.ToString() + "/");
            }

            server.AddResponseUrl("/" + key + "/finish/", (Func<WebServer, HttpListenerRequest, string>)SendFinished);
            System.Diagnostics.Debug.WriteLine("/" + key + "/finish/");
        }

        private string SendFinished(WebServer sender, HttpListenerRequest request)
        {
            try
            {
                var query = new WwwFormUrlDecoder(request.Url.Query);

                var success = (query.GetFirstValueByName("success").ToLower() == "true");
                var message = "";

                if (!success)
                    message = query.GetFirstValueByName("message");

                fileSendTcs.SetResult(message);
            }
            catch (Exception ex)
            {
                fileSendTcs.SetResult(ex.Message);
            }

            return "OK";
        }

        private async Task<byte[]> GetFileSlice(WebServer sender, HttpListenerRequest request)
        {
            try
            {
                string[] parts = request.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];
                ulong id = ulong.Parse(parts[1]);

                if (id >= keyTable[key].lastPieceAccessed)
                {
                    FileTransferProgress?.Invoke(this, new FileTransferProgressEventArgs { CurrentPart = id + 1, Total = keyTable[key].lastSliceId + 1 });
                    keyTable[key].lastPieceAccessed = (uint)id;
                }
                
                StorageFile file = keyTable[key].storageFile;

                int pieceSize = ((keyTable[key].lastSliceId != id) || (keyTable[key].lastSliceSize == 0)) ? (int)Constants.FileSliceMaxLength : (int)keyTable[key].lastSliceSize;

                byte[] buffer = new byte[pieceSize];

                using (Stream stream = (await file.OpenReadAsync()).AsStreamForRead())
                {
                    stream.Seek((int)(id * Constants.FileSliceMaxLength), SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, pieceSize);
                }

                return buffer;
            }
            catch
            {
                return "Invalid Request".Select(c => (byte)c).ToArray();
            }
        }

        private void InitServer()
        {
            if (server != null)
                server.Dispose();

            server = new WebServer(ipFinderResult.MyIP, Constants.CommunicationPort);
        }

        private async Task Handshake()
        {
            ipFinder.IPDetectionCompleted += IpFinder_IPDetectionCompleted;
            ipFinderTcs = new TaskCompletionSource<bool>();
            await ipFinder.StartFindingMyLocalIP();
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
            server.Dispose();
        }
    }
}
