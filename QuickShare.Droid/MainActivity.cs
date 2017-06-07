using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Collections.ObjectModel;
using QuickShare.Droid.RomeComponent;
using System.Collections.Generic;
using QuickShare.DevicesListManager;
using Microsoft.ConnectedDevices;
using Android.Webkit;
using System.Linq;
using Android.Content;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using QuickShare.Droid.Services;
using QuickShare.Droid.OnlineServiceHelpers;
using Firebase.Iid;
using Firebase;
using System.Threading;

namespace QuickShare.Droid
{
    [Activity(Label = "QuickShare.Droid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        DevicesListAdapter devicesAdapter;
        ListView listView;

        private WebView _webView;
        internal Dialog _authDialog;

        static bool IsInitialized = false;

        bool isUserSelectedRemoteSystemManually = false;
        int remoteSystemPrevCount = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            devicesAdapter = new DevicesListAdapter(this, Common.ListManager);
            listView = FindViewById<ListView>(Resource.Id.listView1);
            listView.Adapter = devicesAdapter;
            listView.ItemClick += ListView_ItemClick;
            listView.ItemSelected += ListView_ItemSelected;

            //FindViewById<Button>(Resource.Id.button3).Click += Button3_Click;
            //FindViewById<Button>(Resource.Id.mainSendMessageCarrier).Click += SendMessageCarrier_Click;
            var mainLayout = FindViewById<LinearLayout>(Resource.Id.mainLayout);
            var clipboardButton = FindViewById<Button>(Resource.Id.clipboardButton);
            var sendFileButton = FindViewById<Button>(Resource.Id.sendFileButton);
            var sendPictureButton = FindViewById<Button>(Resource.Id.sendPictureButton);

            clipboardButton.Click += SendClipboard_Click;
            sendFileButton.Click += SendFile_Click;

            mainLayout.ViewTreeObserver.GlobalLayout += (ss, ee) =>
            {
                int x = Math.Min(mainLayout.Width, mainLayout.Height);
                sendFileButton.SetWidth((int)(x * 0.5));
                sendFileButton.SetHeight((int)(x * 0.5));
                clipboardButton.SetWidth((int)(x * 0.25));
                clipboardButton.SetHeight((int)(x * 0.25));
                sendPictureButton.SetWidth((int)(x * 0.25));
                sendPictureButton.SetHeight((int)(x * 0.25));
            };

            UpdateSelectedRemoteSystem();

            if (IsInitialized)
                return;
            IsInitialized = true;

            //var firebaseToken = FirebaseInstanceId.Instance.Token;

            Common.PackageManager = new RomePackageManager(this);
            Common.PackageManager.Initialize("com.quickshare.service");

            Common.MessageCarrierPackageManager = new RomePackageManager(this);
            Common.MessageCarrierPackageManager.Initialize("com.quickshare.messagecarrierservice");

            Common.PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

            InitDiscovery();

            Task.Run(async () =>
            {
#if DEBUG
                FirebaseInstanceId.Instance.DeleteInstanceId();
#endif

                await ServiceFunctions.RegisterDevice();
            });
        }

        private void SendMessageCarrier_Click(object sender, EventArgs e)
        {
            StartService(new Intent(this, typeof(MessageCarrierService)));
        }

        private void SendFile_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "File");
            StartActivity(intent);
        }

        //WebServerComponent.WebServer web = new WebServerComponent.WebServer();
        //private void InitWebServer()
        //{
        //    var ip = NetworkHelper.GetLocalIPAddress();
        //    Toast.MakeText(this, ip, ToastLength.Long).Show();

        //    web.StartWebServer(ip, 8081);
        //}

        private void SendClipboard_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Clipboard");
            StartActivity(intent);

            /**
            System.Diagnostics.Debug.WriteLine("Connect()");
            var result = await packageManager.Connect(GetCurrentRemoteSystem(), false);
            System.Diagnostics.Debug.WriteLine($"Connect() finished with result {result.ToString()}");
            //Toast.MakeText(this, result.ToString(), ToastLength.Short).Show();

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Receiver", "System" },
                { "Platform", "Android" }
            };
            var sendResult = await packageManager.Send(data);
            System.Diagnostics.Debug.WriteLine($"Send finished with result {sendResult.ToString()}");
            Toast.MakeText(this, sendResult.ToString(), ToastLength.Long);
            /**/
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            isUserSelectedRemoteSystemManually = true;

            Common.ListManager.Select(Common.ListManager.RemoteSystems[e.Position]);
            UpdateSelectedRemoteSystem();
        }

        private void UpdateSelectedRemoteSystem()
        {
            if (Common.ListManager.SelectedRemoteSystem == null)
                return;

            RunOnUiThread(() =>
            {
                FindViewById<TextView>(Resource.Id.selectedDeviceName).Text = Common.ListManager.SelectedRemoteSystem.DisplayName;
                System.Diagnostics.Debug.WriteLine(Common.ListManager.SelectedRemoteSystem.DisplayName + " is selected.");
            });
        }

        private void ListView_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            e.View.Selected = true;
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            RemoteSystem rs = Common.GetCurrentRemoteSystem();

            if (rs == null)
            {
                Toast.MakeText(this, "Device not found.", ToastLength.Long).Show();
                return;
            }

            var result = await Common.PackageManager.LaunchUri(new Uri("http://www.ghiasi.net"), rs);
            Toast.MakeText(this, result.ToString(), ToastLength.Long).Show();

            var c = await Common.PackageManager.Connect(rs, false);
            //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
            Common.PackageManager.CloseAppService();
            c = await Common.PackageManager.Connect(rs, false);

            //Common.PeriodicalPing();
            Dictionary<string, object> data = new Dictionary<string, object>()
            {
                {"TestLongRunning", "TestLongRunning" },
            };
            await Common.PackageManager.Send(data);
        }

        private async void InitDiscovery()
        {
            Platform.FetchAuthCode += Platform_FetchAuthCode;
            await Common.PackageManager.InitializeDiscovery();
            await Common.MessageCarrierPackageManager.InitializeDiscovery();
        }

        System.Timers.Timer finishLoadingTimer = null;
        Object finishLoadingLock = new Object();
        private void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                {
                    Common.ListManager.AddDevice(item);
                }

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                {
                    Common.ListManager.RemoveDevice(item);
                }

            if ((Common.ListManager.RemoteSystems.Count > 0) && (!isUserSelectedRemoteSystemManually) && (Common.ListManager.RemoteSystems.Count > remoteSystemPrevCount))
            {
                remoteSystemPrevCount = Common.ListManager.RemoteSystems.Count;
                Common.ListManager.SelectHighScoreItem();
                UpdateSelectedRemoteSystem();
            }

            try
            {
                lock (finishLoadingLock)
                {
                    if (finishLoadingTimer?.Enabled == true)
                    {
                        System.Diagnostics.Debug.WriteLine("Timer stopped!");
                        finishLoadingTimer.Stop();
                        finishLoadingTimer = null;
                    }
                    if (finishLoadingTimer == null)
                    {
                        finishLoadingTimer = new System.Timers.Timer()
                        {
                            AutoReset = false,
                            Interval = 5000,
                        };
                        finishLoadingTimer.Elapsed += FinishLoadingTimer_Elapsed;
                    }

                    System.Diagnostics.Debug.WriteLine("Timer started.......");
                    finishLoadingTimer.Start();
                }
            }
            catch { }

            //finishCheckCtr++;
            //MaybeFinishedLoadingDevices(finishCheckCtr);
        }

        private void FinishLoadingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Timer has done its job :]");
            finishLoadingTimer.Enabled = false;
            FinishedLoadingDevices();
        }

        private async void FinishedLoadingDevices()
        {
            var Ids = Common.PackageManager.RemoteSystems.Where(x => x.Kind.Value != RemoteSystemKinds.Unknown.Value).Select(x => x.Id);
            await ServiceFunctions.RegisterWinDeviceIds(Ids);
        }

        private void Platform_FetchAuthCode(string oauthUrl)
        {
            if (!oauthUrl.ToLower().Contains("user.read"))
                oauthUrl = oauthUrl.Replace("&scope=", "&scope=User.Read+");
            if (!oauthUrl.ToLower().Contains("device.read"))
                oauthUrl = oauthUrl.Replace("&scope=", "&scope=Device.Read+");

            RunOnUiThread(() =>
            {
                _authDialog = new Dialog(this);

                var linearLayout = new LinearLayout(_authDialog.Context);
                _webView = new WebView(_authDialog.Context);
                linearLayout.AddView(_webView);
                _authDialog.SetContentView(linearLayout);

                _webView.SetWebChromeClient(new WebChromeClient());
                _webView.Settings.JavaScriptEnabled = true;
                _webView.Settings.DomStorageEnabled = true;
                _webView.LoadUrl(oauthUrl);

                _webView.SetWebViewClient(new MsaWebViewClient(this));
                _authDialog.Show();
                _authDialog.SetCancelable(true);
            });
        }
    }
}

