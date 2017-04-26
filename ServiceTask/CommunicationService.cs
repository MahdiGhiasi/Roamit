using Newtonsoft.Json;
using PCLStorage;
using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace QuickShare.ServiceTask
{
    public sealed class CommunicationService : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private AppServiceConnection _appServiceconnection;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (details?.Name == "com.quickshare.service") //Remote Activation
            {
                DataStore.DataStorageProviders.Init(Windows.Storage.ApplicationData.Current.LocalFolder.Path);

                _appServiceconnection = details.AppServiceConnection;
                _appServiceconnection.RequestReceived += OnRequestReceived;
                _appServiceconnection.ServiceClosed += AppServiceconnection_ServiceClosed;
                FileTransfer.FileReceiver.FileTransferProgress += FileReceiver_FileTransferProgress;
                TextTransfer.TextReceiver.TextReceiveFinished += TextReceiver_TextReceiveFinished;
                taskInstance.Canceled += OnTaskCanceled;
            }

        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_deferral != null)
            {
                Debug.WriteLine("SERVICETASK CANCELED!");
                Debug.WriteLine(reason);
                // Complete the service deferral.
                _deferral.Complete();
                _deferral = null;
            }
        }

        private void AppServiceconnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            if (_deferral != null)
            {
                // Complete the service deferral.
                _deferral.Complete();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.ContainsKey("Receiver"))
            {
                var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                if (!futureAccessList.ContainsItem("downloadMainFolder"))
                    return;

                IFolder downloadFolder = new WinRTFolder(await futureAccessList.GetFolderAsync("downloadMainFolder"));


                string receiver = args.Request.Message["Receiver"] as string;

                Dictionary<string, object> reqMessage = new Dictionary<string, object>();

                foreach (var item in args.Request.Message)
                {
                    reqMessage.Add(item.Key, item.Value);
                }

                if (receiver == "ServerIPFinder")
                {
                    await FileTransfer.ServerIPFinder.ReceiveRequest(reqMessage);
                }
                else if (receiver == "FileReceiver")
                {
                    await FileTransfer.FileReceiver.ReceiveRequest(reqMessage, downloadFolder);
                }
                else if (receiver == "TextReceiver")
                {
                    TextTransfer.TextReceiver.ReceiveRequest(reqMessage);
                }
                else if (receiver == "System")
                {

                    if (args.Request.Message.ContainsKey("FinishService"))
                        if (_deferral != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Goodbye");
                            _appServiceconnection.Dispose();
                            _deferral.Complete();
                        }
                }
            }
            /* else if (args.Request.Message.ContainsKey("Test"))
             {
                 string s = args.Request.Message["Test"] as string;

                 if (s == null)
                     s = "null";

                 ValueSet vs = new ValueSet();
                 vs.Add("RecvSuccessful", "RecvSuccessful");
                 await args.Request.SendResponseAsync(vs);

                 await System.Threading.Tasks.Task.Delay(1500);

                 ToastFunctions.SendToast(s);
             }
             else if (args.Request.Message.ContainsKey("TestLongRunning"))
             {
                 for (int i = 0; i < 10000; i++)
                 {
                     ToastFunctions.SendToast((i * 5).ToString() + " seconds");
                     await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                 }
             }*/
        }

        private AppServiceConnection notificationService = null;
        private bool connectingToNotificationService = false;
        private async Task<bool> ConnectToNotificationService()
        {
            if (connectingToNotificationService)
                return false;
            connectingToNotificationService = true;


            if (this.notificationService == null)
            {

                this.notificationService = new AppServiceConnection();

                // Here, we use the app service name defined in the app service provider's Package.appxmanifest file in the <Extension> section.
                this.notificationService.AppServiceName = "com.quickshare.notificationservice";

                // Use Windows.ApplicationModel.Package.Current.Id.FamilyName within the app service provider to get this value.
                this.notificationService.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;

                Debug.WriteLine("Connecting to notification service...");
                var status = await this.notificationService.OpenAsync();

                if (status != AppServiceConnectionStatus.Success)
                {
                    Debug.WriteLine("Failed to connect to notification service: " + status);
                    connectingToNotificationService = false;
                    return false;
                }
                Debug.WriteLine("Connected to notification service.");
            }

            connectingToNotificationService = false;
            return true;
        }

        private async void FileReceiver_FileTransferProgress(FileTransfer.FileTransferProgressEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Progress " + e.CurrentPart + "/" + e.Total);

            if (!await ConnectToNotificationService())
                return;


            try
            {
                // Call the service.
                var message = new ValueSet();
                message.Add("Type", "FileTransferProgress");
                message.Add("Data", JsonConvert.SerializeObject(e));

                AppServiceResponse response = await this.notificationService.SendMessageAsync(message);

                if (response.Status != AppServiceResponseStatus.Success)
                {
                    Debug.WriteLine("Failed to send message to notification service: " + response.Status);
                    notificationService = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to send message to notification service (an exception was thrown): " + ex.ToString());
                notificationService = null;
            }
        }

        private async void TextReceiver_TextReceiveFinished(TextTransfer.TextReceiveEventArgs e)
        {
            if (!await ConnectToNotificationService())
                return;

            // Call the service.
            var message = new ValueSet();
            message.Add("Type", "TextReceive");
            message.Add("Data", JsonConvert.SerializeObject(e));

            AppServiceResponse response = await this.notificationService.SendMessageAsync(message);

            if (response.Status != AppServiceResponseStatus.Success)
                Debug.WriteLine("Failed to send message to notification service: " + response.Status);
        }
    }
}
