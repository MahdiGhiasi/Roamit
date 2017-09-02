using GoogleAnalytics;
using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Popups;

namespace QuickShare.HelperClasses.Version
{
    internal static class TrialHelper
    {
        static readonly string RemoveAdsAndSizeLimit_Token = "RemoveAdsAndSizeLimit";
        static readonly string RemoveAdsAndSizeLimit_StoreID = "9msqqgzbc1s5";

        public delegate void ShowUpgradeFlyoutEventHandler(UpgradeFlyoutState state);
        public static event ShowUpgradeFlyoutEventHandler ShowUpgradeFlyout;

        private static StoreContext context = null;

        public static TaskCompletionSource<bool> UpgradeFlyoutCompletion;

        internal static async Task AskForUpgradeWhileSending()
        {
            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.WhileSendingFile);
            await UpgradeFlyoutCompletion.Task;
        }

        internal static async Task AskForUpgrade()
        {
            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.Default);
            await UpgradeFlyoutCompletion.Task;
        }

        public static async Task TryUpgrade()
        {
            if (context == null)
                context = StoreContext.GetDefault();

            try
            {
                StorePurchaseResult result = await context.RequestPurchaseAsync(RemoveAdsAndSizeLimit_StoreID);

                Debug.WriteLine($"In app purchase of {RemoveAdsAndSizeLimit_Token} finished with status: {result.Status}");

                CheckIfFullVersion();

#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("TryUpgrade", "Upgraded", result.Status.ToString()).Build());
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"In app purchase of {RemoveAdsAndSizeLimit_Token} failed: {ex.Message}");
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("TryUpgrade", "Failed", ex.Message).Build());
#endif
            }
            
        }

        internal static async void CheckIfFullVersion()
        {
            if (context == null)
                context = StoreContext.GetDefault();

            StoreAppLicense appLicense = await context.GetAppLicenseAsync();

            if ((appLicense == null) ||
                (appLicense.AddOnLicenses == null) || (appLicense.AddOnLicenses.Count == 0))
            {
                TrialSettings.IsTrial = false;
                return;
            }
            
            foreach (KeyValuePair<string, StoreLicense> item in appLicense.AddOnLicenses)
            {
                if (item.Value.InAppOfferToken == RemoveAdsAndSizeLimit_Token)
                {
                    if (item.Value.IsActive)
                        TrialSettings.IsTrial = false;
                    else
                        TrialSettings.IsTrial = true;
                }
            }
        }
    }

    public enum UpgradeFlyoutState
    {
        Default,
        WhileSendingFile
    }
}
