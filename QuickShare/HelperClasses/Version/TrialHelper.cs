using GoogleAnalytics;
using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;

namespace QuickShare.HelperClasses.Version
{
    internal static class TrialHelper
    {
        static readonly string Token_RemoveAdsAndSizeLimit = "RemoveAdsAndSizeLimit";

        static LicenseInformation licenseInformation;

        public delegate void ShowUpgradeFlyoutEventHandler(UpgradeFlyoutState state);
        public static event ShowUpgradeFlyoutEventHandler ShowUpgradeFlyout;

        public static TaskCompletionSource<bool> UpgradeFlyoutCompletion;

        static TrialHelper()
        {
            try
            {
#if DEBUG
                licenseInformation = CurrentAppSimulator.LicenseInformation;
#else
                licenseInformation = CurrentApp.LicenseInformation;
#endif
            }
            catch { }
        }

        internal static async Task AskForUpgradeWhileSending()
        {
            if (licenseInformation == null)
                return;

            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.WhileSendingFile);
            await UpgradeFlyoutCompletion.Task;
        }

        internal static async Task AskForUpgrade()
        {
            if (licenseInformation == null)
                return;

            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.Default);
            await UpgradeFlyoutCompletion.Task;
        }

        public static async Task TryUpgrade()
        {
            if (licenseInformation == null)
                return;

            if (!licenseInformation.ProductLicenses[Token_RemoveAdsAndSizeLimit].IsActive)
            {
                try
                {
#if DEBUG
                    var result = await CurrentAppSimulator.RequestProductPurchaseAsync(Token_RemoveAdsAndSizeLimit);
#else
                    var result = await CurrentApp.RequestProductPurchaseAsync(Token_RemoveAdsAndSizeLimit);
#endif

                    CheckIfFullVersion();

#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("TryUpgrade", "Upgraded").Build());
#endif
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"In app purchase of {Token_RemoveAdsAndSizeLimit} failed: {ex.Message}");
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("TryUpgrade", "Failed", ex.Message).Build());
#endif
                }
            }
        }

        internal static void CheckIfFullVersion()
        {
            if (licenseInformation == null)
                return;

            if (licenseInformation.ProductLicenses[Token_RemoveAdsAndSizeLimit].IsActive)
                TrialSettings.IsTrial = false;
            else
                TrialSettings.IsTrial = true;
        }
    }

    public enum UpgradeFlyoutState
    {
        Default,
        WhileSendingFile
    }
}
