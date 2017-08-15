using QuickShare.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml;

namespace QuickShare
{
    sealed partial class App : Application
    {
        public static EventHandler PCExtensionAccountIdSet;
        public static EventHandler PCExtensionLoginFailed;

        public static PCExtensionPurpose PCExtensionCurrentPurpose = PCExtensionPurpose.Default;

        public enum PCExtensionPurpose
        {
            Default,
            Genocide,
            ForgetEverything,
        }

        private async void OnPCAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            try
            {
                if (!args.Request.Message.ContainsKey("Action"))
                    throw new Exception("Invalid message received.");

                string action = args.Request.Message["Action"] as string;
                if (action == "SetAccountId")
                {
                    SecureKeyStorage.SetAccountId(args.Request.Message["AccountId"] as string);
                    PCExtensionAccountIdSet?.Invoke(this, new EventArgs());
                }
                else if (action == "LoginFailed")
                {
                    ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"] = false.ToString();
                    PCExtensionLoginFailed?.Invoke(this, new EventArgs());
                }
                else if (action == "WhyImHere")
                {
                    if (PCExtensionCurrentPurpose == PCExtensionPurpose.Default)
                    {
                        bool shouldItRun = false;
                        if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SendCloudClipboard"))
                            bool.TryParse(ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"].ToString(), out shouldItRun);

                        var status = await args.Request.SendResponseAsync(new Windows.Foundation.Collections.ValueSet
                        {
                            { "Answer", shouldItRun ? "Alone" : "Die" },
                        });
                    }
                    else if (PCExtensionCurrentPurpose == PCExtensionPurpose.Genocide)
                    {
                        var status = await args.Request.SendResponseAsync(new Windows.Foundation.Collections.ValueSet
                        {
                            { "Answer", "Genocide" },
                        });
                    }
                    else if (PCExtensionCurrentPurpose == PCExtensionPurpose.ForgetEverything)
                    {
                        var status = await args.Request.SendResponseAsync(new Windows.Foundation.Collections.ValueSet
                        {
                            { "Answer", "ForgetEverything" },
                        });
                    }

                    PCExtensionCurrentPurpose = PCExtensionPurpose.Default;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception on OnPCAppServiceRequestReceived: {ex.Message}");
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        private void PCAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            pcAppServiceDeferral?.Complete();
        }

        private void OnPCAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            pcAppServiceDeferral?.Complete();
        }
    }
}
