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
            AndroidRomePackageManager.Instance.Mode = AndroidRomePackageManager.AndroidPackageManagerMode.PushNotification;
        }
    }
}
