using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service
{
    public static class CloudClipboardService
    {
        public static async Task<bool> SetCloudClipboardActivation(string accountId, string deviceId, bool value)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Constants.ServerAddress}/v2/User/{accountId}/{deviceId}/CloudClipboardActivation/?value={value}");
                    var s = await result.Content.ReadAsStringAsync();

                    if (s == "done")
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SetCloudClipboardActivation: {ex.Message}");
                return false;
            }
        }

        public static async Task<List<DeviceInformation>> GetDevices(string accountId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Constants.ServerAddress}/v2/User/{accountId}/Graph/Devices");
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

        public static async Task SendCloudClipboard(string accountId, string text, string deviceName)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("accountId", accountId ),
                        new KeyValuePair<string, string>("senderName", deviceName),
                        new KeyValuePair<string, string>("text", text),
                    });
                    var response = await httpClient.PostAsync($"{Constants.ServerAddress}/v2/Graph/SendCloudClipboard", formContent);

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

        public static async Task<PremiumStatus> GetPremiumStatus(string accountId)
        {
            try
            {
                PremiumStatus output;

                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Constants.ServerAddress}/v2/User/{accountId}/PremiumStatus");
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

        public static async Task<string> GetUserName(string accountId)
        {
            try
            {
                string userName = "";

                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Constants.ServerAddress}/v2/User/{accountId}/UserName");
                    userName = await result.Content.ReadAsStringAsync();
                }

                return userName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in GetUserName: {ex.Message}");
                return null;
            }
        }
    }

    public class DeviceInformation
    {
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string FormFactor { get; set; }
        public string Status { get; set; }
        public bool CloudClipboardEnabled { get; set; }
    }

    public class PremiumStatus
    {
        public AccountPremiumState State { get; set; }
        public DateTime TrialExpireTime { get; set; } = DateTime.MaxValue;
    }

    public enum AccountPremiumState
    {
        Free = 1,
        Premium = 2,
        PremiumTrial = 3,
    }
}
