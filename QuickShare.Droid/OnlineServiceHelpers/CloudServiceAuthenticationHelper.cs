using System;
using System.Threading.Tasks;
using Plugin.SecureStorage;
using QuickShare.Common.Service;
using QuickShare.Common.Service.Models;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal static class CloudServiceAuthenticationHelper
    {
        static CloudServiceAuthenticationHelper()
        {
            SecureStorageImplementation.StoragePassword = Config.Secrets.SecureStoragePassword;
        }

        public static bool IsAuthenticatedForApiV1()
        {
            return CrossSecureStorage.Current.HasKey("UserUniqueId");
        }

        public static bool IsAuthenticatedForApiV2()
        {
            return CrossSecureStorage.Current.HasKey("RoamitAccountId");
        }

        public static bool IsAuthenticatedForApiV3()
        {
            return CrossSecureStorage.Current.HasKey("RoamitAccountToken");
        }

        public static async Task MigrateFromV1ToV3()
        {
            if (!IsAuthenticatedForApiV1())
                throw new Exception("User is not authenticated for API v1.");

            var loginInfo = await QuickShare.Common.Service.Device.MigrateToV3(CrossSecureStorage.Current.GetValue("UserUniqueId"));

            if (loginInfo == null)
                throw new Exception("Failed to migrate to API v3.");

            CrossSecureStorage.Current.SetValue("RoamitAccountId", loginInfo.AccountId.ToString());
            CrossSecureStorage.Current.SetValue("RoamitAccountToken", loginInfo.Token);
        }

        public static APIv3LoginInfo GetApiLoginInfo()
        {
            return new APIv3LoginInfo(Guid.Parse(CrossSecureStorage.Current.GetValue("RoamitAccountId")),
                CrossSecureStorage.Current.GetValue("RoamitAccountToken"));
        }

        public static void LogOut()
        {
            if (CrossSecureStorage.Current.HasKey("UserUniqueId"))
                CrossSecureStorage.Current.DeleteKey("UserUniqueId");

            if (CrossSecureStorage.Current.HasKey("RoamitAccountId"))
                CrossSecureStorage.Current.DeleteKey("RoamitAccountId");

            if (CrossSecureStorage.Current.HasKey("RoamitAccountToken"))
                CrossSecureStorage.Current.DeleteKey("RoamitAccountToken");
        }
    }
}