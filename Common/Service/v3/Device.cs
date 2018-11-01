using Newtonsoft.Json;
using QuickShare.DevicesListManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.v3
{
    public class Device : ServiceBase
    {
        public Device(Guid accountId, string token) :
            base("v3", "Device", accountId, token)
        {
        }

        public async Task<IEnumerable<NormalizedRemoteSystem>> GetDevices()
        {
            string responseText = "";

            try
            {

                var response = await SendGetRequest("Devices");
                responseText = await response.Content.ReadAsStringAsync();

                var devices = JsonConvert.DeserializeObject<List<Models.v3.DeviceBasic3>>(responseText);

                var output = from d in devices
                             //where d.Type == DeviceType.Android || d.Type == DeviceType.GraphWindowsDevice
                             select new NormalizedRemoteSystem
                             {
                                 Id = d.DeviceID,
                                 DisplayName = d.Name,
                                 Kind = d.FormFactor ?? (d.Kind == DeviceType.Android ? "Android" : ""),
                                 Status = NormalizedRemoteSystemStatus.Available,
                                 IsAvailableByProximity = false,
                                 IsAvailableBySpatialProximity = false,
                                 AppVersion = d.AppVersion ?? "",
                                 Type = d.Kind,
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

        public async Task<string> ChangeCloudClipboardActivation(string deviceId, bool value)
        {
            var response = await SendGetRequest($"CloudClipboardActivation/{deviceId}", new []
            {
                new KeyValuePair<string, string>("value", value.ToString()),
            });
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> LaunchUri(string deviceId, string url)
        {
            try
            {
                var response = await SendPostRequest($"LaunchUri/{deviceId}", new Dictionary<string, object>
                {
                    { "url", url },
                }, HttpPostContentType.FormUrlEncoded);
                var result = await response.Content.ReadAsStringAsync();

                return result.Split(',')[0] == "1";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Device.LaunchUri failed: " + ex.ToString());
                return false;
            }
        }

        public async Task<bool> SendCommand(string deviceId, Dictionary<string, object> payload, string appServiceName = "")
        {
            try
            {
                var response = await SendPostRequest($"SendCommand/{deviceId}", new Dictionary<string, object>
                {
                    { "appServiceName", appServiceName },
                    { "payload", payload },
                }, HttpPostContentType.Json);
                var result = await response.Content.ReadAsStringAsync();

                return result.Split(',')[0] == "1";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Device.SendCommand failed: " + ex.ToString());
                return false;
            }
        }

        public async Task<bool> SendClipboard(string deviceId, string text, string senderName)
        {
            try
            {
                var response = await SendPostRequest($"SendClipboard/{deviceId}", new Dictionary<string, object>
                {
                    { "text", text },
                    { "senderName", senderName },
                }, HttpPostContentType.FormUrlEncoded);
                var result = await response.Content.ReadAsStringAsync();

                return result.Split(',')[0] == "1";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Device.SendClipboard failed: " + ex.ToString());
                return false;
            }
        }
    }
}
