using QuickShare.UWP.Rome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    internal static class PackageManagerHelper
    {
        internal static void InitAndroidPackageManagerMode()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("LegacyAndroidMode"))
            {
                AndroidRomePackageManager.Instance.Mode = AndroidRomePackageManager.AndroidPackageManagerMode.PushNotification;
                return;
            }

            var legacyAndroidMode = (ApplicationData.Current.LocalSettings.Values["LegacyAndroidMode"] as bool?) ?? false;

            if (legacyAndroidMode == true)
                AndroidRomePackageManager.Instance.Mode = AndroidRomePackageManager.AndroidPackageManagerMode.MessageCarrier;
            else
                AndroidRomePackageManager.Instance.Mode = AndroidRomePackageManager.AndroidPackageManagerMode.PushNotification;
        }
    }
}
