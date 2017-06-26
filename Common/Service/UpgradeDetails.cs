using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service
{
    public static class UpgradeDetails
    {
        public static async Task<bool> SetUpgradeStatus(string userId, bool isFullVersion)
        {
            try
            {
                var httpClient = new HttpClient();
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("isFullVersion", isFullVersion.ToString()),
                });
                var response = await httpClient.PostAsync($"{Constants.ServerAddress}/api/User/{userId}/SetAccountDetails", formContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from SetUpgradeStatus: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in SetUpgradeStatus: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<VersionStatus> GetUpgradeStatus(string userId)
        {
            try
            {
                var httpClient = new HttpClient();
                
                var response = await httpClient.GetAsync($"{Constants.ServerAddress}/api/User/{userId}/IsFullVersion");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText == "0")
                    return VersionStatus.TrialVersion;
                else if (responseText == "1")
                    return VersionStatus.FullVersion;
                else
                    return VersionStatus.Unknown;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in GetUpgradeStatus: {ex.Message}");
                return VersionStatus.Unknown;
            }
        }

        public enum VersionStatus
        {
            TrialVersion = 1,
            FullVersion = 2,
            Unknown = 3,
        }
    }
}
