using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;

namespace QuickShare.Rome
{
    public class RomePackageManager
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

            if (rs.IsAvailableByProximity)
                return tryCountProximity;
            else
                return tryCountCloud;
        }

        RomeHelper romeHelper = null;

        public ObservableCollection<RemoteSystem> RemoteSystems
        {
            get
            {
                return romeHelper.RemoteSystems;
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
                if (result != AppServiceConnectionStatus.Success)
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

        public async Task<AppServiceConnectionStatus> Connect(RemoteSystem _remoteSystem, bool keepAlive)
        {
            try
            {
                keepCurrentConnectionAlive = keepAlive;


                AppServiceConnectionStatus result = AppServiceConnectionStatus.Unknown;
                bool workDone;

                int tryCount = getTryCount(_remoteSystem);
                for (int i = 0; i < tryCount; i++)
                {
                    workDone = false;

                    Task connect = Task.Run(async () =>
                    {
                        if (romeHelper == null)
                        {
                            remoteSystem = _remoteSystem;
                        }
                        else
                        {
                            remoteSystem = romeHelper.RemoteSystems.FirstOrDefault(x => x.Id == _remoteSystem.Id);
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
                    Task delay = SetTimeoutTask(_remoteSystem, i);

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
                return result;
            }
            catch (Exception ex)
            {
                return AppServiceConnectionStatus.Unknown;
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

        public async Task<RemoteLaunchUriStatus> LaunchStoreForApp()
        {
            var result = await LaunchUri(new Uri(@"ms-windows-store://pdp/?PFN=" + appService.PackageFamilyName));

            return result;
        }

        public async Task<RemoteLaunchUriStatus> LaunchUri(Uri uri)
        {
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

                    launchStatus = await Windows.System.RemoteLauncher.LaunchUriAsync(connectionRequest, uri/*, options*/);
                    workDone = true;
                }, false);

                Task delay = SetTimeoutTask(remoteSystem, i);

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


            return launchStatus;
        }

        public async Task<AppServiceResponse> Send(ValueSet data)
        {
            AppServiceResponse response = null;
            int tryCount = getTryCount();
            for (int i = 0; i < tryCount; i++)
            {
                response = await Send(data, i);
                if (response != null)
                    break;

                System.Diagnostics.Debug.WriteLine("Send failed.");
            }
            return response;
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
