using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    internal static class Service
    {
        internal static async Task<bool> SetCloudClipboardActivation(string accountId, string deviceId, bool value)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Config.ServerAddress}/v2/User/{accountId}/{deviceId}/CloudClipboardActivation/?value={value}");
                    var s = await result.Content.ReadAsStringAsync();

                    if (s == "done")
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetPremiumStatus: {ex.Message}");
                return false;
            }
        }

        internal static async Task<List<DeviceInformation>> GetDevices(string accountId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Config.ServerAddress}/v2/User/{accountId}/Graph/Devices");
                    var s = await result.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<List<DeviceInformation>>(s);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetPremiumStatus: {ex.Message}");
                return null;
            }
        }

        internal static async Task SendCloudClipboard(string accountId, string text)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string deviceName = CurrentDevice.GetDeviceName();

                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("accountId", accountId ),
                        new KeyValuePair<string, string>("senderName", deviceName),
                        new KeyValuePair<string, string>("text", text),
                    });
                    var response = await httpClient.PostAsync($"{Config.ServerAddress}/v2/Graph/SendCloudClipboard", formContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(responseText);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to send: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SendCloudClipboard: {ex.Message}");
            }
        }

        internal static async Task<PremiumStatus> GetPremiumStatus(string accountId)
        {
            try
            {
                PremiumStatus output;

                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Config.ServerAddress}/v2/User/{accountId}/PremiumStatus");
                    var s = await result.Content.ReadAsStringAsync();

                    var parts = s.Split(',');

                    if (parts[0].ToLower() == "free")
                        output = new PremiumStatus
                        {
                            State = AccountPremiumState.Free,
                        };
                    else if (parts[0].ToLower() == "premiumtrial")
                        output = new PremiumStatus
                        {
                            State = AccountPremiumState.PremiumTrial,
                            TrialExpireTime = new DateTime(long.Parse(parts[1])),
                        };
                    else
                        output = new PremiumStatus
                        {
                            State = AccountPremiumState.Premium,
                        };
                }

                return output;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetPremiumStatus: {ex.Message}");
                return null;
            }
        }
    }

    internal class DeviceInformation
    {
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string FormFactor { get; set; }
        public string Status { get; set; }
        public bool CloudClipboardEnabled { get; set; }
    }

    internal class PremiumStatus
    {
        internal AccountPremiumState State { get; set; }
        internal DateTime TrialExpireTime { get; set; } = DateTime.MaxValue;
    }

    internal enum AccountPremiumState
    {
        Free = 1,
        Premium = 2,
        PremiumTrial = 3,
    }
}
