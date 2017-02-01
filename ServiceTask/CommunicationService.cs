using QuickShare.Common;
using System;
using System.Collections.Generic;
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
                _appServiceconnection = details.AppServiceConnection;
                _appServiceconnection.RequestReceived += OnRequestReceived;
                _appServiceconnection.ServiceClosed += AppServiceconnection_ServiceClosed;
            }
        }


        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_deferral != null)
            {
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
                if (args.Request.Message["Receiver"] as string == "ServerIPFinder")
                {
                    await FileSendReceive.ServerIPFinder.ReceiveRequest(args.Request);
                }
            }
            else if (args.Request.Message.ContainsKey("Test"))
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
            }


            if (_deferral != null)
                _deferral.Complete();
        }
    }
}
