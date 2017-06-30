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
using Plugin.SecureStorage;
using System.Threading.Tasks;
using QuickShare.Common.Service;
using static QuickShare.Common.Service.UpgradeDetails;

namespace QuickShare.Droid
{
    internal static class TrialHelper
    {
        public delegate void UserTrialStatusChangedEventHandler();
        public static event UserTrialStatusChangedEventHandler UserTrialStatusChanged;

        static TrialHelper()
        {
            SecureStorageImplementation.StoragePassword = Config.Secrets.SecureStoragePassword;
        }

        internal static VersionStatus UserTrialStatus
        {
            get
            {
                if ((CrossSecureStorage.Current.HasKey("UserTrialStatus")) && (int.TryParse(CrossSecureStorage.Current.GetValue("UserTrialStatus"), out int trialStatus)))
                {
                    return (VersionStatus)trialStatus;
                }
                return VersionStatus.Unknown;
            }
        }

        internal static async Task<VersionStatus> RefreshUserTrialStatusAsync(string userId)
        {
            var status = await UpgradeDetails.GetUpgradeStatus(userId);
            var oldStatus = UserTrialStatus;

            CrossSecureStorage.Current.SetValue("UserTrialStatus", ((int)status).ToString());

            if (status != oldStatus)
                UserTrialStatusChanged?.Invoke();

            return status;
        }
    }
}