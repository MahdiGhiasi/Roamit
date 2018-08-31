using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QuickShare.Droid.RomeComponent;
using Microsoft.ConnectedDevices;
using System.Threading.Tasks;
using QuickShare.DevicesListManager;
using QuickShare.Droid.Classes;

namespace QuickShare.Droid
{
    internal static class Common
    {
        internal static RomePackageManager PackageManager { get; set; } = null;
        internal static RomePackageManager MessageCarrierPackageManager { get; set; } = null;
        internal static RoamitCloudPackageManager AndroidPushNotifier { get; set; } = null;
        internal static DevicesListManager.DevicesListManager ListManager { get; } = new DevicesListManager.DevicesListManager("", new RemoteSystemNormalizer());

        internal static string ShareText { get; set; }
        internal static string[] ShareFiles { get; set; }

        internal static RemoteSystem GetCurrentRemoteSystem()
        {
            var nrs = Common.ListManager.SelectedRemoteSystem;
            var rs = Common.PackageManager.RemoteSystems.FirstOrDefault(x => x.Id == nrs?.Id);
            return rs;
        }

        internal static RemoteSystem GetCurrentRemoteSystemForMessageCarrier()
        {
            var nrs = Common.ListManager.SelectedRemoteSystem;
            var rs = Common.MessageCarrierPackageManager.RemoteSystems.FirstOrDefault(x => x.Id == nrs?.Id);
            return rs;
        }

        internal static NormalizedRemoteSystem GetCurrentNormalizedRemoteSystem()
        {
            return Common.ListManager.SelectedRemoteSystem;
        }

        private static bool pingTimerEnable = false;
        internal static async void PeriodicalPing()
        {
            pingTimerEnable = true;

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"Receiver", "System"},
                {"Task", "Ping" },
            };

            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                
                var result = await Common.PackageManager.Send(data);
                System.Diagnostics.Debug.WriteLine("Pinged remote system.");
            }
            while (pingTimerEnable);
        }

        internal static void FinishPeriodicalPing()
        {
            pingTimerEnable = false;
        }
    }
}