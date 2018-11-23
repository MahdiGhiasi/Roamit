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
    public class User : ServiceBase
    {
        private object deviceType;

        public User(Guid accountId, string token) : 
            base("v3", "User", accountId, token)
        {
        }

        public async Task<string> GetUserName()
        {
            var response = await SendGetRequest("UserName");
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IEnumerable<NormalizedRemoteSystem>> GetDevices()
        {
            var response = await SendGetRequest("Devices");
            var responseText = await response.Content.ReadAsStringAsync();

            var devices = JsonConvert.DeserializeObject<List<Models.v3.DeviceBasic3>>(responseText);

            return ParseDevices(devices);
        }

        public async Task<IEnumerable<NormalizedRemoteSystem>> GetDevices(DeviceType kind)
        {
            var response = await SendGetRequest($"Devices/{kind.ToString()}");
            var responseText = await response.Content.ReadAsStringAsync();

            var devices = JsonConvert.DeserializeObject<List<Models.v3.DeviceBasic3>>(responseText);

            return ParseDevices(devices);
        }

        private IEnumerable<NormalizedRemoteSystem> ParseDevices(List<Models.v3.DeviceBasic3> devices)
        {
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

        public async Task<bool> HasDevicesPermission()
        {
            var response = await SendGetRequest("CheckDevicesPermission");
            var result = await response.Content.ReadAsStringAsync();

            if (result.Contains(',') && result.Split(',')[0] == "1")
                return true;
            return false;
        }

        public async Task<bool> RegisterDevice(string deviceName, string osVersion, string deviceUniqueId, string type, string firebaseToken, string appVersion)
        {
            try
            { 
                var response = await SendPostRequest("RegisterDevice", new Dictionary<string, object>
                {
                    { "name", deviceName },
                    { "osVersion", osVersion },
                    { "deviceId", deviceUniqueId },
                    { "type", deviceType },
                    { "token", firebaseToken },
                    { "appVersion", appVersion },
                }, HttpPostContentType.FormUrlEncoded);

                var responseText = await response.Content.ReadAsStringAsync();

                return ((responseText == "1, registered") || (responseText == "2, updated"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RegisterDevice failed. " + ex.ToString());
                return false;
            }
        }

        public async Task<bool> RegisterWinDeviceIds(string[] ids)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(ids);
                var response = await SendPostRequest("WIDS", jsonData, "application/json");
                var responseText = await response.Content.ReadAsStringAsync();
                return (responseText == "1, success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RegisterWinDeviceIds failed. " + ex.ToString());
                return false;
            }
        }

        public async Task<bool> RemoveDevice(string deviceUniqueId)
        {
            try
            {
                var response = await SendPostRequest("RemoveDevice", new Dictionary<string, object>
                {
                    { "deviceId", deviceUniqueId },
                }, HttpPostContentType.FormUrlEncoded);

                var responseText = await response.Content.ReadAsStringAsync();

                return (responseText == "1, removed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RemoveDevice failed. " + ex.ToString());
                return false;
            }
        }
    }
}
