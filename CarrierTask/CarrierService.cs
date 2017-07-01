using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace QuickShare.CarrierTask
{

    //
    // Direct connection from Android device to a service in Main process caused
    // some weird crashes. This proxy probably reduces it.
    // 
    public sealed class CarrierService : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        AppServiceConnection appServiceConnection;
        AppServiceConnection carrierInternalService;

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (details?.Name == "com.roamit.messagecarrierservice") //Remote Activation
            {
                appServiceConnection = details.AppServiceConnection;
                appServiceConnection.RequestReceived += OnRequestReceived;
                appServiceConnection.ServiceClosed += AppServiceconnection_ServiceClosed;
                taskInstance.Canceled += OnTaskCanceled;

                Debug.WriteLine("MessageCarrierService is starting...");

                await semaphoreSlim.WaitAsync();

                if (!(await ConnectToInternalCarrierService()))
                {
                    semaphoreSlim.Release();
                    deferral.Complete();
                    return;
                }
                semaphoreSlim.Release();

                Debug.WriteLine("MessageCarrierService started.");
            }

        }

        private async Task<bool> ConnectToInternalCarrierService()
        {
            if (carrierInternalService != null)
                return true;

            carrierInternalService = new AppServiceConnection()
            {
                AppServiceName = "com.roamit.carrierinternal",
                PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
            };

            var status = await carrierInternalService.OpenAsync();

            if (status != AppServiceConnectionStatus.Success)
            {
                Debug.WriteLine("Failed to connect to notification service: " + status);
                return false;
            }

            return true;
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (deferral != null)
            {
                Debug.WriteLine($"MessageCarrierService cancelled because of {reason.ToString()}.");
                // Complete the service deferral.
                deferral.Complete();
                deferral = null;
            }
        }

        private void AppServiceconnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            if (deferral != null)
            {
                Debug.WriteLine($"MessageCarrierService closed.");
                // Complete the service deferral.
                deferral.Complete();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var requestDeferral = args.GetDeferral();

            await semaphoreSlim.WaitAsync();

            await ConnectToInternalCarrierService();

            var message1 = new ValueSet();
            foreach (var item in args.Request.Message)
            {
                message1.Add(item.Key, item.Value);
            }

            AppServiceResponse response = await carrierInternalService.SendMessageAsync(message1);
            if (response.Status == AppServiceResponseStatus.Success)
            {
                Debug.WriteLine("Message proxy went well.");

                var message2 = new ValueSet();
                foreach (var item in response.Message)
                {
                    message2.Add(item.Key, item.Value);
                }

                await args.Request.SendResponseAsync(message2);
            }
            else
            {
                Debug.WriteLine("**** Internal carrier communication gone wrong :(");
            }

            semaphoreSlim.Release();

            requestDeferral.Complete();
        }
    }
}
