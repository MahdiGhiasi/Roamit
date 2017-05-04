using Android.Content;
using Microsoft.ConnectedDevices;
using QuickShare.Common.Rome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Droid.RomeComponent
{
    public class RomePackageManager : IRomePackageManager
    {
        readonly string appIdentifier = "";
        string appServiceName = "";

        AppServiceClientConnection appService;
        RemoteSystem remoteSystem;
        RemoteSystemConnectionRequest connectionRequest;
        RomeHelper romeHelper = new RomeHelper();
        AppServiceConnectionListener connectionListener = new AppServiceConnectionListener();
        AppServiceResponseListener responseListener = new AppServiceResponseListener();

        public ObservableCollection<RemoteSystem> RemoteSystems
        {
            get
            {
                return romeHelper?.RemoteSystems ?? new ObservableCollection<RemoteSystem>();
            }
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive)
        {
            return await Connect(_remoteSystem, keepAlive, null);
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive, Uri wakeUri)
        {
            var rs = _remoteSystem as RemoteSystem;
            
            appService = new AppServiceClientConnection(appServiceName,
                appIdentifier,
                new RemoteSystemConnectionRequest(rs),
                connectionListener,
                responseListener);

            return RomeAppServiceConnectionStatus.Unknown;
        }

        public void Initialize(string _appServiceName)
        {
            appServiceName = _appServiceName;
            
        }

        public async Task InitializeDiscovery(Context appContext)
        {
            await romeHelper.InitializeAsync(appContext);
        }

        public Task<RomeRemoteLaunchUriStatus> LaunchStoreForApp()
        {
            throw new NotImplementedException();
        }

        public async Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri)
        {
            return await LaunchUri(uri, remoteSystem);
        }

        public async Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri, object remoteSystemOverride)
        {
            RemoteSystem rs = null;
            if (remoteSystemOverride != null)
            {
                rs = remoteSystemOverride as RemoteSystem;
                if (rs == null)
                    throw new InvalidCastException();
            }

            var launchUriStatus = await RemoteLauncher.LaunchUriAsync(new RemoteSystemConnectionRequest(remoteSystem), uri);
            
            return launchUriStatus.ConvertToRomeRemoteLaunchUriStatus();
        }

        public Task<RomeAppServiceResponse> Send(Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }
    }
}
