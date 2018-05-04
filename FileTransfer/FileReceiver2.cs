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

        public delegate void ReceiveFileProgressEventHandler(FileTransfer2ProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        static ReceiveSessionAgent currentReceiveSessionAgent;

        public static async Task<Dictionary<string, object>> ReceiveRequest(Dictionary<string, object> request, IDownloadFolderDecider downloadFolderDecider, Func<string, Task<IFolder>> folderResolver)
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
                        await ProcessRequest(request, fileSenderVersion, downloadFolderDecider, folderResolver, isResume: false);
                        return new Dictionary<string, object>();
                    case "ResumeReceive":
                        Debug.WriteLine("Received ResumeReceive request. TODO.");
                        if (currentReceiveSessionAgent != null)
                        {
                            currentReceiveSessionAgent.Stop();
                            await Task.Delay(500);
                        }
                        await ProcessRequest(request, fileSenderVersion, downloadFolderDecider, folderResolver, isResume: true);
                        return new Dictionary<string, object>();
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

        private static async Task ProcessRequest(Dictionary<string, object> request, int fileSenderVersion, IDownloadFolderDecider downloadFolderDecider, Func<string, Task<IFolder>> folderResolver, bool isResume)
        {
            var sessionKey = Guid.Parse(request["Guid"] as string);
            var ip = request["ServerIP"].ToString();
            var isCompatible = CompatibilityHelper.IsCompatible(fileSenderVersion, fileReceiverVersion);
            var senderName = request["SenderName"].ToString();

            if ((!isResume) && (fileSenderVersion >= 2))
                await SendVersionCheckGetRequestAsync(ip, sessionKey, isCompatible);

            if (!isCompatible)
            {
                // TODO
                return;
            }

            currentReceiveSessionAgent = new ReceiveSessionAgent(ip, sessionKey, senderName, downloadFolderDecider, folderResolver);
            currentReceiveSessionAgent.FileTransferProgress += (e) =>
            {
                FileTransferProgress?.Invoke(e);
            };

            currentReceiveSessionAgent.StartReceive(isResume);
            await currentReceiveSessionAgent.ReceiveFinishTcs.Task;
        }

        private static async Task SendVersionCheckGetRequestAsync(string ip, Guid sessionKey, bool isCompatible)
        {
            try
            {
                await HttpHelper.SendGetRequestAsync($"http://{ip}:{Constants.CommunicationPort}/{sessionKey.ToString()}/versionCheck/?receiverVersion={fileReceiverVersion}&receiverCompatible={(isCompatible ? "true" : "false")}");
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
