using QuickShare.FileTransfer.Helpers;
using Microsoft.AspNetCore.WebUtilities;
using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    public class FileSender2 : IDisposable
    {
        readonly int fileSenderVersion = 2;
        readonly int maxRetrySignal = 1;

        private int fileReceiverVersion = 1;
        private object remoteSystem;
        private IWebServerGenerator webServerGenerator;
        private IRomePackageManager packageManager;
        private Handshaker handshaker;
        private string deviceName;
        private TaskCompletionSource<FileTransferResult> transferTcs, timeoutTcs;

        public delegate void FileTransferProgressEventHandler(object sender, FileTransfer2ProgressEventArgs e);
        public event FileTransferProgressEventHandler FileTransferProgress;

        public FileSender2(object remoteSystem, IWebServerGenerator webServerGenerator, IRomePackageManager packageManager, IEnumerable<string> myIPs, string deviceName)
        {
            this.remoteSystem = remoteSystem;
            this.webServerGenerator = webServerGenerator;
            this.packageManager = packageManager;

            this.handshaker = new Handshaker(this.webServerGenerator, this.packageManager, myIPs);

            this.deviceName = deviceName;
        }

        public void Dispose()
        {
            handshaker.Dispose();
        }

        private IWebServer InitServer(string ip, int port)
        {
            var server = webServerGenerator.GenerateInstance();
            server.StartWebServer(ip, port);

            return server;
        }

        // files must be a list to make the function predictable. (As we modify the FileSendInfo objects by calling their InitSlicingAsync() funciton) 
        public async Task<FileTransferResult> Send(List<FileSendInfo> files, CancellationToken cancellationToken = default(CancellationToken))
        {
            IWebServer server = null;

            var transferProgress = new FileSendProgressCalculator(Constants.FileSliceMaxLength);
            transferProgress.FileTransferProgress += TransferProgress_FileTransferProgress;

            try
            {
                transferTcs = new TaskCompletionSource<FileTransferResult>();
                timeoutTcs = new TaskCompletionSource<FileTransferResult>();
                var sessionKey = Guid.NewGuid().ToString();

                var myIp = await handshaker.Handshake(cancellationToken);

                if (myIp.Length == 0)
                    return FileTransferResult.FailedOnHandshake;
                if (cancellationToken.IsCancellationRequested)
                    return FileTransferResult.Cancelled;

                server = InitServer(myIp, Constants.CommunicationPort);
                await InitFileReceiveEndpoints(server, files, transferProgress, cancellationToken);
                await InitGenericEndpoints(server, sessionKey, myIp, files, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return FileTransferResult.Cancelled;

                return await SendFiles(sessionKey, myIp, transferProgress, cancellationToken);
            }
            finally
            {
                transferProgress.FileTransferProgress -= TransferProgress_FileTransferProgress;
                server?.Dispose();
            }
        }

        private async Task InitGenericEndpoints(IWebServer server, string sessionKey, string ip, IEnumerable<FileSendInfo> files, CancellationToken cancellationToken = default(CancellationToken))
        {
            await InitQueueDataEndpoint(server, sessionKey, ip, files);
            InitFinishQueueEndpoint(server, sessionKey);
            InitVersionCheckEndpoint(server, sessionKey);
        }

        private void InitVersionCheckEndpoint(IWebServer server, string sessionKey)
        {
            server.AddResponseUrl($"/{sessionKey}/versionCheck/", (Func<IWebServer, RequestDetails, string>)VersionCheck);
        }

        private string VersionCheck(IWebServer server, RequestDetails request)
        {
            try
            {
                var query = QueryHelpers.ParseQuery(request.Url.Query);
                fileReceiverVersion = int.Parse(query["receiverVersion"][0]);
                var receiverCompatible = (query["receiverCompatible"][0].ToLower() == "true");
                var compatible = receiverCompatible && CompatibilityHelper.IsCompatible(fileSenderVersion, fileReceiverVersion);
                
                if (!compatible)
                    throw new Exception($"Not compatible (sender: {fileSenderVersion}, receiver: {fileReceiverVersion} - {receiverCompatible})");
                
                return "true";
            }
            catch (Exception ex)
            {
                //TODO
                return "false";
            }
        }

        private void InitFinishQueueEndpoint(IWebServer server, string sessionKey)
        {
            server.AddResponseUrl($"/{sessionKey}/finishQueue/", (Func<IWebServer, RequestDetails, string>)FinishQueue);
        }

        private string FinishQueue(IWebServer server, RequestDetails request)
        {
            transferTcs.TrySetResult(FileTransferResult.Successful);
            return "Ok";
        }

        private static async Task InitQueueDataEndpoint(IWebServer server, string sessionKey, string ip, IEnumerable<FileSendInfo> files)
        {
            FileInfoListGenerator generator = new FileInfoListGenerator(files, ip);
            var queueData = await generator.GenerateAsync();
            server.AddResponseUrl($"/{sessionKey}/", queueData.FileInfoListJsonLegacy);
            server.AddResponseUrl($"/{sessionKey}/queueInfo/", queueData.FileInfoListJson);
        }

        private void TransferProgress_FileTransferProgress(object sender, FileTransfer2ProgressEventArgs e)
        {
            FileTransferProgress?.Invoke(this, e);
        }

        private async Task InitFileReceiveEndpoints(IWebServer server, IEnumerable<FileSendInfo> files, FileSendProgressCalculator transferProgress, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var item in files)
            {
                await InitFileReceiveEndpoints(server, item, transferProgress);

                if (cancellationToken.IsCancellationRequested)
                    return;
            }

            var list = files.ToList();
        }

        private async Task InitFileReceiveEndpoints(IWebServer server, FileSendInfo fileInfo, FileSendProgressCalculator transferProgress)
        {
            await fileInfo.InitSlicingAsync();

            var fileSliceSender = new FileSliceSender(fileInfo);
            transferProgress.AddFileSliceSender(fileSliceSender);
            fileSliceSender.SliceRequested += transferProgress.SliceRequestReceived;

            for (int i = 0; i < fileInfo.SlicesCount; i++)
            {
                server.AddResponseUrl($"/{fileInfo.UniqueKey}/{i}/", (Func<IWebServer, RequestDetails, Task<byte[]>>)fileSliceSender.GetFileSlice);
            }
        }

        private async Task<FileTransferResult> SendFiles(string sessionKey, string ip, FileSendProgressCalculator transferProgress, CancellationToken cancellationToken = default(CancellationToken))
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SendInitReceiverMessage(transferProgress.TotalSlices, sessionKey, ip, false);
#pragma warning restore CS4014

            transferProgress.InitTimeout(timeoutTcs);

            var cancellationTcs = new TaskCompletionSource<FileTransferResult>();
            var cancellationRegistration = cancellationToken.Register(s => ((TaskCompletionSource<FileTransferResult>)s).SetResult(FileTransferResult.Cancelled), cancellationTcs);

            try
            {
                while (true)
                {
                    var result = (await Task.WhenAny(transferTcs.Task, timeoutTcs.Task, cancellationTcs.Task)).Result;
                    if (result == FileTransferResult.Timeout && fileReceiverVersion >= 2)
                    {
                        if (transferProgress.TransferStarted)
                        {
                            // Resume Request
                            FileTransferProgress?.Invoke(this, new FileTransfer2ProgressEventArgs
                            {
                                State = FileTransferState.Reconnecting,
                            });
                            await packageManager.Connect();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            SendInitReceiverMessage(transferProgress.TotalSlices, sessionKey, ip, isResume: true);
#pragma warning restore CS4014

                            FileTransferProgress?.Invoke(this, new FileTransfer2ProgressEventArgs
                            {
                                State = FileTransferState.Reconnected,
                            });

                            timeoutTcs = new TaskCompletionSource<FileTransferResult>();
                            transferProgress.InitTimeout(timeoutTcs);
                        }
                        else
                        {
                            return FileTransferResult.FailedOnPrepare;
                        }
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            finally
            {
                cancellationRegistration.Dispose();
            }
        }

        private async Task<bool> SendInitReceiverMessage(long totalSlices, string key, string ip, bool isResume)
        {
            // This list must stay backward compatible with older versions of FileReceiver.
            Dictionary<string, object> qInit = new Dictionary<string, object>
            {
                { "Receiver", "FileReceiver" },
                { "Type", isResume ? "ResumeReceive" : "QueueInit" },
                { "TotalSlices", (long)totalSlices },
                { "QueueFinishKey", key },
                { "ServerIP", ip },
                { "Guid", key },
                { "SenderName", deviceName },
                { "parentDirectoryName", "" },
                { "QueueInfoKey", key },
                { "FileSenderVersion", fileSenderVersion.ToString() },
            };
            var result = await packageManager.Send(qInit);

            if (result.Status == RomeAppServiceResponseStatus.Success)
                return true;
            else
            {
                Debug.WriteLine($"SendInitReceiverMessage: Send failed ({result.Status.ToString()})");
                return false;
            }
        }
    }
}
