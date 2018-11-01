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


        Common.Service.v3.Device device = null;
        List<NormalizedRemoteSystem> remoteSystems = null;

        string deviceId = null;

        public bool IsInitialized => device != null;

        public async Task Initialize(APIv3LoginInfo loginInfo)
        {
            device = new Common.Service.v3.Device(loginInfo.AccountId, loginInfo.Token);
            remoteSystems = (await device.GetDevices()).Where(x => x.Type == DeviceType.GraphWindowsDevice || x.Type == DeviceType.Android).ToList();
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

        private string FindDevice(string deviceName)
        {
            var candidates = remoteSystems.Where(x => x.DisplayName.ToLower() == deviceName.ToLower()).ToArray();

            if (candidates.Length == 1)
                return candidates[0].Id;

            return null;
        }
    }
}
