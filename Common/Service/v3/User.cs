using Newtonsoft.Json;
using QuickShare.DevicesListManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.v3
{
    public class User : ServiceBase
    {
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
    }
}
