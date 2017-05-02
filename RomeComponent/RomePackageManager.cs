using QuickShare.Common.Rome;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using System.Collections.Generic;

namespace QuickShare.UWP.Rome
{
    public class RomePackageManager : IRomePackageManager
    {
        //Singleton class
        static RomePackageManager _instance = null;
        public static RomePackageManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RomePackageManager();

                return _instance;
            }
        }

        private RomePackageManager() { }

        AppServiceConnection appService;
        RemoteSystem remoteSystem;
        RemoteSystemConnectionRequest connectionRequest;
        bool keepCurrentConnectionAlive = false;

        internal int tryCountProximity = 6;
        internal int tryCountCloud = 2;

        private int getTryCount(RemoteSystem rs = null)
        {
            if (rs == null)
                rs = remoteSystem;

            if (rs == null)
                return tryCountCloud;
            else if (rs.IsAvailableByProximity)
                return tryCountProximity;
            else
                return tryCountCloud;
        }

        RomeHelper romeHelper = null;

        public ObservableCollection<RemoteSystem> RemoteSystems
        {
            get
            {
                return romeHelper?.RemoteSystems ?? new ObservableCollection<RemoteSystem>();
            }
        }

        public async Task InitializeDiscovery()
        {
            if (romeHelper == null)
            {
                romeHelper = new RomeHelper();
                await romeHelper.Initialize();
            }
        }

        private async Task ReinitializeDiscovery()
        {
            romeHelper = new RomeHelper();
            await romeHelper.Initialize();
        }

        public void Initialize(string appServiceName)
        {
            Initialize(appServiceName, Windows.ApplicationModel.Package.Current.Id.FamilyName);
        }

        private void Initialize(string appServiceName, string packageFamilyName)
        {
            appService = new AppServiceConnection()
            {
                AppServiceName = appServiceName,
                PackageFamilyName = packageFamilyName
            };
            connectionRequest = null;
        }

        private void ResetAppService()
        {
            string serviceName = appService.AppServiceName;
            string packageName = appService.PackageFamilyName;

            appService.RequestReceived -= AppService_RequestReceived;
            appService.Dispose();

            appService = new AppServiceConnection()
            {
                AppServiceName = serviceName,
                PackageFamilyName = packageName
            };
        }

        private async void AppService_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Connection closed. " + args.Status);
            await ReconnectIfEnabled();
        }

        private async Task ReconnectIfEnabled()
        {
            if (keepCurrentConnectionAlive)
            {
                System.Diagnostics.Debug.WriteLine("Reconnecting...");
                var result = await Connect(remoteSystem, keepCurrentConnectionAlive);
                if (result != RomeAppServiceConnectionStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine("Reconnect failed. " + result);
                }
            }
        }

        private async void AppService_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            System.Diagnostics.Debug.WriteLine("Received!");
            await (new MessageDialog("RECEIVED!")).ShowAsync();
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive)
        {
            return await Connect(_remoteSystem, keepAlive, null);
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive, Uri wakeUri)
        {
            RemoteSystem rs = _remoteSystem as RemoteSystem;
            if (rs == null)
                throw new InvalidCastException();

            try
            {
                keepCurrentConnectionAlive = keepAlive;


                AppServiceConnectionStatus result = AppServiceConnectionStatus.Unknown;
                bool workDone;

                int tryCount = getTryCount(rs);
                for (int i = 0; i < tryCount; i++)
                {
                    workDone = false;

                    if ((i == 0) && (wakeUri != null) && (!rs.IsAvailableByProximity))
                    {
                        //Wake device first
                        var wakeResult = await LaunchUri(wakeUri, rs);

                        if (wakeResult == RomeRemoteLaunchUriStatus.Success)
                        {
                            await ReinitializeDiscovery();

                            int count = 0;
                            RemoteSystem rsNew = null;
                            while (rsNew == null)
                            {
                                rsNew = romeHelper.RemoteSystems.FirstOrDefault(x => x.Id == rs.Id);
                                count++;

                                if (count > 20)
                                    return RomeAppServiceConnectionStatus.RemoteSystemUnavailable;

                                await Task.Delay(50);
                            }
                        }
                    }
                    
                    Task connect = Task.Run(async () =>
                    {
                        if (romeHelper == null)
                        {
                            remoteSystem = rs;
                        }
                        else
                        {
                            remoteSystem = romeHelper.RemoteSystems.FirstOrDefault(x => x.Id == rs.Id);
                            if (remoteSystem == null)
                            {
                                result = AppServiceConnectionStatus.RemoteSystemUnavailable;
                                workDone = true;
                                return;
                            }
                        }

                        ResetAppService();
                        AppServiceConnection curService = appService;

                        connectionRequest = new RemoteSystemConnectionRequest(remoteSystem);
                        result = await appService.OpenRemoteAsync(connectionRequest);
                        workDone = true;

                        if (appService != curService)
                            result = AppServiceConnectionStatus.Unknown;
                    });
                    Task delay = SetTimeoutTask(rs, i);

                    await Task.WhenAny(new Task[] { connect, delay });

                    if (workDone)
                        break;
                    else
                        System.Diagnostics.Debug.WriteLine("Connecting timeout");

                }


                if (result == AppServiceConnectionStatus.Success)
                {
                    appService.RequestReceived += AppService_RequestReceived;
                    appService.ServiceClosed += AppService_ServiceClosed;
                }
                return (RomeAppServiceConnectionStatus)result;
            }
            catch
            {
                return RomeAppServiceConnectionStatus.Unknown;
            }
        }

        private static Task SetTimeoutTask(RemoteSystem _remoteSystem, int tryi)
        {
            Task delay;
            if (_remoteSystem.IsAvailableByProximity)
                delay = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 + tryi, 5))); });
            else
                delay = Task.Run(async () => { await Task.Delay(TimeSpan.FromSeconds(Math.Min(15 + 5 * tryi, 20))); });
            return delay;
        }

        public async Task<RomeRemoteLaunchUriStatus> LaunchStoreForApp()
        {
            var result = await LaunchUri(new Uri(@"ms-windows-store://pdp/?PFN=" + appService.PackageFamilyName));

            return (RomeRemoteLaunchUriStatus)result;
        }

        public async Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri)
        {
            return await LaunchUri(uri, null);
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

            RemoteLaunchUriStatus launchStatus = RemoteLaunchUriStatus.RemoteSystemUnavailable;

            bool workDone = false;
            int tryCount = getTryCount();
            for (int i = 0; i < tryCount; i++)
            {
                Task launch = Common.DispatcherEx.RunOnCoreDispatcherIfPossible(async () =>
                {
                    /*var options = new Windows.System.RemoteLauncherOptions()
                                    {
                                        FallbackUri = new Uri(@"http://google.com")
                                    };*/

                    RemoteSystemConnectionRequest req = connectionRequest;
                    if (rs != null)
                        req = new RemoteSystemConnectionRequest(rs);

                    launchStatus = await Windows.System.RemoteLauncher.LaunchUriAsync(req, uri/*, options*/);
                    workDone = true;
                }, false);

                Task delay = SetTimeoutTask(remoteSystem ?? rs, i);

                await Task.WhenAny(new Task[] { launch, delay });

                if (workDone)
                    break;
                else
                {
                    System.Diagnostics.Debug.WriteLine("Launch timeout");
                    if (i >= 2)
                        await ReconnectIfEnabled();
                }
            }


            return (RomeRemoteLaunchUriStatus)launchStatus;
        }

        public async Task<RomeAppServiceResponse> Send(Dictionary<string, object> data)
        {
            var res = await Connect(remoteSystem, keepCurrentConnectionAlive);

            ValueSet sendData = new ValueSet();
            foreach (var item in data)
                sendData.Add(item.Key, item.Value);

            AppServiceResponse response = null;
            int tryCount = getTryCount();
            for (int i = 0; i < tryCount; i++)
            {
                response = await Send(sendData, i);
                if (response != null)
                    break;

                System.Diagnostics.Debug.WriteLine("Send failed.");
            }
            
            if (response == null)
            {
                return null;
            }
            else
            {
                return new RomeAppServiceResponse()
                {
                    Status = (RomeAppServiceResponseStatus)response.Status,
                    Message = response.Message?.ToDictionary(p => p.Key, p => p.Value)
                };
            }
        }

        private async Task<AppServiceResponse> Send(ValueSet data, int tryi)
        {
            try
            {
                AppServiceResponse response = null;
                Task send = Task.Run(async () => { response = await appService.SendMessageAsync(data); });
                Task delay = SetTimeoutTask(remoteSystem, tryi);

                await Task.WhenAny(new Task[] { send, delay });

                if ((response == null) && (tryi >= 2))
                    await ReconnectIfEnabled();

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in RomePackageManager.Send(): " + ex.Message);
                return null;
            }
        }
    }
}
