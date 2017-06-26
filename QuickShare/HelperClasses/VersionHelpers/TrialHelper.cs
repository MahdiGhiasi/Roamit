using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace QuickShare.HelperClasses.VersionHelpers
{
    internal static class TrialHelper
    {
        internal static async Task AskForUpgradeWhileSending()
        {
            var md = new MessageDialog("You can upgrade to full version to unlock this capability and remove the ads.", $"The free version is limited to sending at most {TrialSettings.MaxSizeForTrialVersion} MB of files each time.");

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
            var md = new MessageDialog($"The free version is limited to sending at most {TrialSettings.MaxSizeForTrialVersion} MB of files each time.\r\nYou can upgrade to full version to unlock this capability and remove the ads.", $"Upgrade to full version");
        
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

        }

        internal static async Task<bool> IsFullVersion()
        {

            return false;
        }
    }
}
