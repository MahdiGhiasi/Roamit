using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QuickShare.Common.Rome;
using Newtonsoft.Json;
using QuickShare.DevicesListManager;
using System.Collections.ObjectModel;
using Microsoft.ConnectedDevices;
using v1 = QuickShare.Common.Service;
using v3 = QuickShare.Common.Service.v3;

namespace QuickShare.Droid.RomeComponent
{
    public class RoamitCloudPackageManager : IRomePackageManager
    {
        string userId, deviceId;
        private v3.User v3EndpointUser;
        private v3.Device v3EndpointDevice;
        private Guid accountId;
        private string token;
        readonly TimeSpan retryDelay = TimeSpan.FromSeconds(10);

        public ObservableCollection<NormalizedRemoteSystem> Devices { get; } = new ObservableCollection<NormalizedRemoteSystem>();

        public void SetLoginInfoV1(string userId)
        {
            this.userId = userId;
            this.accountId = Guid.Empty;
            this.token = "";
            this.v3EndpointUser = null;
            this.v3EndpointDevice = null;
        }

        public void SetLoginInfoV3(Guid accountId, string token)
        {
            this.accountId = accountId;
            this.token = token;
            this.userId = "";
            this.v3EndpointUser = new v3.User(accountId, token);
            this.v3EndpointDevice = new v3.Device(accountId, token);
        }

        public void SetLoginInfoV3(v1.APIv3LoginInfo loginInfo)
        {
            SetLoginInfoV3(loginInfo.AccountId, loginInfo.Token);
        }

        public async void BeginDiscoverDevices(string currentDeviceUniqueId)
        {
            IEnumerable<NormalizedRemoteSystem> devices = null;
            while (devices == null)
            {
                if (v3EndpointUser != null) // API v3 available 
                    devices = await v3EndpointUser.GetDevices();
                else
                    devices = await v1.Device.GetAndroidDevices(userId);
               
                if (devices == null)
                    await Task.Delay(retryDelay);
            }

            AddDevicesToList(currentDeviceUniqueId, devices.Where(x => x.Type == DeviceType.Android || x.Type == DeviceType.GraphWindowsDevice));
        }

        private void AddDevicesToList(string currentDeviceUniqueId, IEnumerable<NormalizedRemoteSystem> devices)
        {
            foreach (var item in devices)
            {
                if (item.Id == currentDeviceUniqueId)
                    continue;

                var existing = Devices.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                    continue;

                Devices.Add(item);
            }
        }

        public void SetRemoteDevice(string _deviceId)
        {
            deviceId = _deviceId;
        }

        public async Task<RomeAppServiceResponse> Send(Dictionary<string, object> data)
        {
            var dataJson = JsonConvert.SerializeObject(data);
            bool result;
            if (v3EndpointDevice != null) // API v3 available 
                result = await v3EndpointDevice.SendCommand(deviceId, data);
            else
                result = await v1.Device.SendMessage(userId, deviceId, dataJson);

            if (result)
                return new RomeAppServiceResponse
                {
                    Message = null,
                    Status = RomeAppServiceResponseStatus.Success,
                };
            else
                return new RomeAppServiceResponse
                {
                    Message = null,
                    Status = RomeAppServiceResponseStatus.Failure,
                };
        }

        public async Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri _uri, NormalizedRemoteSystem _remoteSystem)
        {
            bool result;
            if (v3EndpointDevice != null) // API v3 available 
                result = await v3EndpointDevice.LaunchUri(_remoteSystem.Id, _uri.OriginalString);
            else
                result = await v1.Device.LaunchUri(userId, _remoteSystem.Id, _uri);

            if (result)
                return RomeRemoteLaunchUriStatus.Success;
            else
                return RomeRemoteLaunchUriStatus.RemoteSystemUnavailable;
        }

        public async Task<bool> QuickClipboard(string text, NormalizedRemoteSystem remoteSystem, string senderName)
        {
            if ((text + senderName).Length > 2100)
                return false;

            bool result;

            if (v3EndpointDevice != null) // API v3 available 
                result = await v3EndpointDevice.SendClipboard(remoteSystem.Id, text, senderName);
            else
                result = await v1.Device.SendClipboard(userId, remoteSystem.Id, text, senderName);

            return result;
        }

        public async Task<RomeAppServiceConnectionStatus> Connect()
        {
            return RomeAppServiceConnectionStatus.Success;
        }
    }
}