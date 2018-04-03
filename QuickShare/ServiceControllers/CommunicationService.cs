using PCLStorage;
using QuickShare.Classes;
using QuickShare.Common;
using QuickShare.FileTransfer;
using QuickShare.HelperClasses;
using QuickShare.ToastNotifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;

namespace QuickShare
{
    sealed partial class App : Application
    {
        private void InitCommunicationService()
        {
            FileReceiver2.ClearEventRegistrations();
            FileReceiver2.FileTransferProgress += FileReceiver_FileTransferProgress;
        }

        private async void FileReceiver_FileTransferProgress(FileTransfer2ProgressEventArgs e)
        {
            if (e.State == FileTransferState.Finished)
            {
                Toaster.ShowFileReceiveFinishedNotification(e.TotalFiles, e.SenderName, e.Guid);
            }
            else if (e.State == FileTransferState.Error)
            {
                Toaster.ShowFileReceiveFailedNotification(e.Guid, e.Exception);
            }

            await NotificationHandler.HandleAsync(e);
        }

        private async void OnCommunicationServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            try
            {
                if (args.Request.Message.ContainsKey("Receiver"))
                {
                    Dictionary<string, object> reqMessage = new Dictionary<string, object>();
                    foreach (var item in args.Request.Message)
                        reqMessage.Add(item.Key, item.Value);

                    await ParseMessage(reqMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception on OnCommunicationServiceRequestReceived: {ex.Message}");
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        private async Task ParseMessage(Dictionary<string, object> reqMessage)
        {
            string receiver = reqMessage["Receiver"] as string;

            if (receiver == "ServerIPFinder")
                await FileTransfer.ServerIPFinder.ReceiveRequest(reqMessage);
            else if (receiver == "FileReceiver")
                await FileTransfer.FileReceiver2.ReceiveRequest(reqMessage, DecideDownloadFolder);
            else if (receiver == "TextReceiver")
                await TextTransfer.TextReceiver.ReceiveRequest(reqMessage);
            else if (receiver == "CloudClipboardHandler")
                CloudClipboardHandler.ReceiveRequest(reqMessage);
        }

        private async Task<IFolder> DecideDownloadFolder(string[] fileTypes)
        {
            return await DownloadFolderDecider.DecideAsync(fileTypes);
        }

        private void CommunicationServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            communicationServiceDeferral?.Complete();
        }

        private void OnCommunicationServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            communicationServiceDeferral?.Complete();
        }
    }
}
