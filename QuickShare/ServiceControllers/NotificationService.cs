using Newtonsoft.Json;
using QuickShare.Common;
using QuickShare.FileTransfer;
using QuickShare.TextTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace QuickShare
{
    sealed partial class App : Application
    {
        private async void OnNotificationAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            try
            {
                ValueSet message = args.Request.Message;

                if (message["Type"].ToString() == "FileTransferProgress")
                {
                    await DispatcherEx.RunOnCoreDispatcherIfPossible(async () =>
                    {
                        await NotificationHandler.HandleAsync(JsonConvert.DeserializeObject<FileTransferProgressEventArgs>(message["Data"] as string));
                    });
                }
                else if (message["Type"].ToString() == "TextReceive")
                {
                    await DispatcherEx.RunOnCoreDispatcherIfPossible(async () =>
                    {
                        await NotificationHandler.HandleAsync(JsonConvert.DeserializeObject<TextReceiveEventArgs>(message["Data"] as string));
                    });
                }

                ValueSet returnMessage = new ValueSet();
                returnMessage.Add("Status", "OK");
                await args.Request.SendResponseAsync(returnMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unhandled exception in OnAppServiceRequestReceived():");
                Debug.WriteLine(ex.ToString());
                await (new MessageDialog(ex.ToString(), "Unhandled exception in OnAppServiceRequestReceived()")).ShowAsync();
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        private void OnNotificationAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            notificationAppServiceDeferral?.Complete();
        }

        private void NotificationAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            notificationAppServiceDeferral?.Complete();
        }
    }
}
