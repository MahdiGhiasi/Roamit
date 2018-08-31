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
using QuickShare.Common.Service;
using QuickShare.DevicesListManager;
using System.Collections.ObjectModel;
using Microsoft.ConnectedDevices;

namespace QuickShare.Droid.RomeComponent
{
    public class RoamitCloudPackageManager : IRomePackageManager
    {
        string userId, deviceId;

        public ObservableCollection<NormalizedRemoteSystem> Devices { get; } = new ObservableCollection<NormalizedRemoteSystem>();

        public RoamitCloudPackageManager(string _userId)
        {
            userId = _userId;
        }

        public async Task DiscoverAndroidDevices(string currentDeviceUniqueId)
        {
            foreach (var item in await Device.GetAndroidDevices(userId))
            {
                if (item.Id == currentDeviceUniqueId)
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

            bool result = await Device.SendMessage(userId, deviceId, dataJson);

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
            bool result = await Device.LaunchUri(userId, _remoteSystem.Id, _uri);

            if (result)
                return RomeRemoteLaunchUriStatus.Success;
            else
                return RomeRemoteLaunchUriStatus.RemoteSystemUnavailable;
        }

        public async Task<bool> QuickClipboard(string text, NormalizedRemoteSystem remoteSystem, string senderName)
        {
            if ((text + senderName).Length > 2100)
                return false;

            bool result = await Device.SendClipboard(userId, remoteSystem.Id, text, senderName);

            return result;
        }

        public async Task<RomeAppServiceConnectionStatus> Connect()
        {
            return RomeAppServiceConnectionStatus.Success;
        }
    }
}