using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;

namespace QuickShare
{
    sealed partial class App : Application
    {
        private void MessageCarrierAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            messageCarrierAppServiceDeferral?.Complete();
        }

        private void OnMessageCarrierAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            messageCarrierAppServiceDeferral?.Complete();
        }

        DateTime lastCall = DateTime.MinValue;
        private async void OnMessageCarrierAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                Debug.WriteLine("A message carrier received. Processing...");
                await MainPage.Current.AndroidPackageManager.MessageCarrierReceivedAsync(args.Request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while processing MessageCarrier.");
                Debug.WriteLine(ex.ToString());
            }

            lastCall = DateTime.Now;
            CheckIfIsOver(lastCall);

            deferral.Complete();
        }

        private async void CheckIfIsOver(DateTime callTime)
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
            if (lastCall != callTime)
                return;

            if (!MainPage.Current.AndroidPackageManager.HasWaitingMessageCarrier)
            {
                Debug.WriteLine("We're done here.");
                messageCarrierAppServiceDeferral.Complete();
            }
        }
    }
}
