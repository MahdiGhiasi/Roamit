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

namespace QuickShare.HelperClasses.VersionHelpers
{
    internal static class TrialHelper
    {
        static readonly string Token_RemoveAdsAndSizeLimit = "RemoveAdsAndSizeLimit";

        static LicenseInformation licenseInformation;

        static TrialHelper()
        {
#if DEBUG
            licenseInformation = CurrentAppSimulator.LicenseInformation;
#else
            licenseInformation = CurrentApp.LicenseInformation;
#endif
        }

        internal static async Task AskForUpgradeWhileSending()
        {
            var md = new MessageDialog("You can upgrade to full version to unlock this capability and remove the ads.", $"The free version is limited to sending at most {Constants.MaxSizeForTrialVersion} MB of files each time.");

            md.Commands.Add(new UICommand("Upgrade") { Id = 0 });
            md.Commands.Add(new UICommand("No, thanks") { Id = 1 });

            md.DefaultCommandIndex = 0;
            md.CancelCommandIndex = 1;

            var result = await md.ShowAsync();
            if (result.Id as int? == 0)
            {
                await TryUpgrade();
            }
        }

        internal static async Task AskForUpgrade()
        {
            var md = new MessageDialog($"The free version is limited to sending at most {Constants.MaxSizeForTrialVersion} MB of files each time.\r\nYou can upgrade to full version to unlock this capability and remove the ads.", $"Upgrade to full version");
        
            md.Commands.Add(new UICommand("Upgrade") { Id = 0 });
            md.Commands.Add(new UICommand("No, thanks") { Id = 1 });

            md.DefaultCommandIndex = 0;
            md.CancelCommandIndex = 1;

            var result = await md.ShowAsync();
            if (result.Id as int? == 0)
            {
                await TryUpgrade();
            }
        }

        private static async Task TryUpgrade()
        {
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
            if (licenseInformation.ProductLicenses[Token_RemoveAdsAndSizeLimit].IsActive)
                TrialSettings.IsTrial = false;
            else
                TrialSettings.IsTrial = true;
        }
    }
}
