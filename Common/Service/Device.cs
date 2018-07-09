using Newtonsoft.Json;
using QuickShare.DevicesListManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service
{
    public static class Device
    {
        public static async Task<IEnumerable<NormalizedRemoteSystem>> GetAndroidDevices(string userId)
        {
            string responseText = "";

            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{Constants.ServerAddress}/api/User/{userId}/Devices/Android");
                responseText = await response.Content.ReadAsStringAsync();

                var devices = JsonConvert.DeserializeObject<List<Models.Device>>(responseText);

                var output = from d in devices
                             select new NormalizedRemoteSystem
                             {
                                 Id = d.DeviceID,
                                 DisplayName = d.FriendlyName,
                                 Kind = "QS_Android",
                                 Status = NormalizedRemoteSystemStatus.Available,
                                 IsAvailableByProximity = false,
                                 IsAvailableBySpatialProximity = false,
                                 AppVersion = d.AppVersion ?? "",
                             };

                return output;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in GetAndroidDevices: {ex.Message}");
                Debug.WriteLine($"Server returned text was '{responseText}'");
                return new List<NormalizedRemoteSystem>();
            }
        }

        public static async Task<bool> WakeAndroidDevices(string userId)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{Constants.ServerAddress}/api/User/{userId}/TryWakeAll");
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from TryWakeAll: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in WakeAndroidDevices: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<bool> RequestMessageCarrier(string userId, string deviceId, IEnumerable<string> whosNotMe)
        {
            try
            {
                var httpClient = new HttpClient();
                var jsonData = JsonConvert.SerializeObject(whosNotMe);
                var response = await httpClient.PostAsync($"{Constants.ServerAddress}/api/User/{userId}/{deviceId}/StartCarrierService", new StringContent(jsonData, Encoding.UTF8, "application/json"));
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from StartCarrierService: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in RequestMessageCarrier: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<bool> SendMessage(string userId, string deviceId, string data)
        {
            try
            {
                var httpClient = new HttpClient();
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Data", data),
                });
                var response = await httpClient.PostAsync($"{Constants.ServerAddress}/api/User/{userId}/{deviceId}/SendMessage", formContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from LaunchUri: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in LaunchUri: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<bool> LaunchUri(string userId, string deviceId, Uri uri)
        {
            try
            {
                var httpClient = new HttpClient();
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Url", uri.OriginalString),
                });
                var response = await httpClient.PostAsync($"{Constants.ServerAddress}/api/User/{userId}/{deviceId}/LaunchUrl", formContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from LaunchUri: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in LaunchUri: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<bool> SendClipboard(string userId, string deviceId, string text, string senderName)
        {
            try
            {
                var httpClient = new HttpClient();
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("senderName", senderName),
                });
                var response = await httpClient.PostAsync($"{Constants.ServerAddress}/api/User/{userId}/{deviceId}/FastClipboard", formContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText != "1, done")
                {
                    Debug.WriteLine($"Received unexpected message from SendClipboard: '{responseText}'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception was thrown in SendClipboard: {ex.Message}");
                return false;
            }

            return true;
        }

        public static async Task<bool> RemoveDevice(string accountId, string deviceId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = await httpClient.GetAsync($"{Constants.ServerAddress}/v2/User/{accountId}/{deviceId}/RemoveDevice");
                    var s = await result.Content.ReadAsStringAsync();

                    if (s == "1, removed")
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in RemoveDevice: {ex.Message}");
                return false;
            }
        }

    }
}
