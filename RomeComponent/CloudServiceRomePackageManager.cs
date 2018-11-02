using Newtonsoft.Json;
using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.Common.Service.Models;
using QuickShare.DevicesListManager;
using QuickShare.Rome;
using QuickShare.UWP.Rome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;

namespace QuickShare.UWP.Rome
{
    public class CloudServiceRomePackageManager : IRomePackageManager
    {
        //Singleton class
        static CloudServiceRomePackageManager _instance = null;
        public static CloudServiceRomePackageManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CloudServiceRomePackageManager();

                return _instance;
            }
        }

        private CloudServiceRomePackageManager() { }


        Common.Service.v3.User user = null;
        Common.Service.v3.Device device = null;
        List<NormalizedRemoteSystem> remoteSystems = null;

        string deviceId = null;

        public bool IsInitialized => user != null;

        public async Task Initialize(Guid accountId, string token)
        {
            user = new Common.Service.v3.User(accountId, token);
            device = new Common.Service.v3.Device(accountId, token);

            TimeSpan delayTime = TimeSpan.FromSeconds(3);
            while (true)
            {
                try
                {
                    remoteSystems = (await user.GetDevices()).Where(x => x.Type == DeviceType.GraphWindowsDevice || x.Type == DeviceType.Android).ToList();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize CloudService package manager: {ex.Message}");
                    await Task.Delay(delayTime);
                    delayTime = TimeSpan.FromSeconds(Math.Min(delayTime.TotalSeconds * 2, 20));
                }

            }
        }

        public async Task<RomeAppServiceResponse> Send(Dictionary<string, object> data)
        {
            if (deviceId == null)
                return new RomeAppServiceResponse
                {
                    Message = null,
                    Status = RomeAppServiceResponseStatus.Failure,
                };

            bool result = await device.SendCommand(deviceId, data);

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

        public async Task<RomeRemoteLaunchUriStatus> LaunchUri(string deviceName, Uri uri)
        {
            var deviceId = FindDevice(deviceName);

            if (deviceId == null)
                return RomeRemoteLaunchUriStatus.RemoteSystemUnavailable;

            bool result = await device.LaunchUri(deviceId, uri.ToString());

            if (result)
                return RomeRemoteLaunchUriStatus.Success;
            else
                return RomeRemoteLaunchUriStatus.RemoteSystemUnavailable;
        }

        public async Task<RomeAppServiceConnectionStatus> Connect()
        {
            if (IsInitialized && deviceId != null)
                return RomeAppServiceConnectionStatus.Success;

            return RomeAppServiceConnectionStatus.AppServiceUnavailable;
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(string deviceName)
        {
            deviceId = FindDevice(deviceName);

            if (deviceId != null)
                return RomeAppServiceConnectionStatus.Success;

            return RomeAppServiceConnectionStatus.RemoteSystemUnavailable;
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(string deviceName, Uri launchUri)
        {
            deviceId = FindDevice(deviceName);

            if (deviceId != null)
            {
                var launchUriResult = await LaunchUri(deviceName, launchUri);

                if (launchUriResult == RomeRemoteLaunchUriStatus.Success)
                    return RomeAppServiceConnectionStatus.Success;
            }

            return RomeAppServiceConnectionStatus.RemoteSystemUnavailable;
        }

        public async Task<bool> QuickClipboardForWindowsDevice(string _text, string deviceName, string _senderName)
        {
            if ((_text + _senderName).Length > 1024)
                return false;

            var uri = new Uri("roamit://clipboard/" + _senderName.EncodeToBase64() + "?" + _text.EncodeToBase64());
            var result = await LaunchUri(deviceName, uri);

            return (result == RomeRemoteLaunchUriStatus.Success);
        }

        private string FindDevice(string deviceName)
        {
            var candidates = remoteSystems.Where(x => x.DisplayName.ToLower() == deviceName.ToLower()).ToArray();

            if (candidates.Length == 1)
                return candidates[0].Id;

            return null;
        }
    }
}
