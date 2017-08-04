using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    internal static class Updater
    {
        internal static async Task CheckForUpdates()
        {
            using (var mgr = new UpdateManager(@"C:\Users\Mahdi\Projects\QuickShare\Releases"))
            {
                Debug.WriteLine("Checking for updates...");

                var updateInfo = await mgr.CheckForUpdate();
                if (updateInfo.ReleasesToApply.Any())
                {
                    Debug.WriteLine($"Update available. Downloading and installing...");
                    await mgr.UpdateApp();

                    Debug.WriteLine("Restarting app...");
                    UpdateManager.RestartApp();
                }
            }
        }
    }
}
