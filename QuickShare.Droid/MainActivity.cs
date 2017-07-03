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
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Views;
using Android.Net;
using QuickShare.Droid.Helpers;

namespace QuickShare.Droid
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.mainpage")]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*", Label = "Roamit")]
    public class MainActivity : AppCompatActivity
    {
        DevicesListAdapter devicesAdapter;
        ListView listView;

        static bool IsInitialized = false;

        bool isUserSelectedRemoteSystemManually = false;
        int remoteSystemPrevCount = 0;

        bool showToolbarMenu = true;

        RelativeLayout mainActions, mainShare;
        Button shareFileBtn, shareUrlBtn, shareTextBtn;
        Button clipboardButton, sendFileButton, sendPictureButton, sendUrlButton;
        TextView clipboardPreviewText;

        bool clipboardButtonEnabled = true;

        private Timer clipboardUpdateTimer;

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

            mainActions = FindViewById<RelativeLayout>(Resource.Id.main_actions);
            mainShare = FindViewById<RelativeLayout>(Resource.Id.main_share);

            RefreshUserTrialStatus();

            if ((Intent.Action == Intent.ActionSend) || (Intent.Action == Intent.ActionSendMultiple))
            {
                OnCreate_Share();
            }
            else
            {
                OnCreate_Main();
            }

            SetButtonsEnableStatus(false);

            UpdateSelectedRemoteSystem();

            if (IsInitialized)
                return;
            IsInitialized = true;

            Common.PackageManager = new RomePackageManager(this);
            Common.PackageManager.Initialize("com.roamit.service");

            Common.MessageCarrierPackageManager = new RomePackageManager(this);
            Common.MessageCarrierPackageManager.Initialize("com.roamit.messagecarrierservice");

            Common.PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

            InitDiscovery();
        }

        private async void RefreshUserTrialStatus()
        {
            if (MSAAuthenticator.HasUserUniqueId())
            {
                var userId = await MSAAuthenticator.GetUserUniqueIdAsync();
                await TrialHelper.RefreshUserTrialStatusAsync(userId);
            }
        }

        private void SetButtonsEnableStatus(bool enabled)
        {
            if (shareFileBtn != null)
                shareFileBtn.Enabled = enabled;
            if (shareUrlBtn != null)
                shareUrlBtn.Enabled = enabled;
            if (shareTextBtn != null)
                shareTextBtn.Enabled = enabled;
            if (clipboardButton != null)
                clipboardButton.Enabled = enabled && clipboardButtonEnabled;
            if (sendFileButton != null)
                sendFileButton.Enabled = enabled;
            if (sendPictureButton != null)
                sendPictureButton.Enabled = enabled;
            if (sendUrlButton != null)
                sendUrlButton.Enabled = enabled;
        }

        private void OnCreate_Share()
        {
            showToolbarMenu = false;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            //Toolbar will now take on default actionbar characteristics
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Share to device";

            mainActions.Visibility = ViewStates.Gone;
            mainShare.Visibility = ViewStates.Visible;

            var contentPreview = FindViewById<TextView>(Resource.Id.main_txt_shareContent);
            shareFileBtn = FindViewById<Button>(Resource.Id.main_btn_share_file);
            shareUrlBtn = FindViewById<Button>(Resource.Id.main_btn_share_url);
            shareTextBtn = FindViewById<Button>(Resource.Id.main_btn_share_text);

            shareFileBtn.Visibility = ViewStates.Gone;
            shareUrlBtn.Visibility = ViewStates.Gone;
            shareTextBtn.Visibility = ViewStates.Gone;

            if ((Intent.Action == Intent.ActionSend) && (Intent.Extras.ContainsKey(Intent.ExtraStream)))
            {
                var fileUrl = FilePathHelper.GetPath(this, (Android.Net.Uri)Intent.Extras.GetParcelable(Intent.ExtraStream));

                contentPreview.Text = fileUrl;
                Common.ShareFiles = new string[] { fileUrl };

                shareFileBtn.Visibility = ViewStates.Visible;
            }
            else if (Intent.Action == Intent.ActionSendMultiple && Intent.Extras.ContainsKey(Intent.ExtraStream))
            {
                string[] urls = Intent.Extras.GetParcelableArrayList(Intent.ExtraStream)
                    .Cast<Android.Net.Uri>()
                    .Select(x => FilePathHelper.GetPath(this, x))
                    .ToArray();

                contentPreview.Text = urls.Length + " files";
                Common.ShareFiles = urls;

                shareFileBtn.Visibility = ViewStates.Visible;
            }
            else if ((Intent.Action == Intent.ActionSend) && (Intent.Type == "text/plain"))
            {
                string sharedText = Intent.GetStringExtra(Intent.ExtraText);

                contentPreview.Text = sharedText;
                Common.ShareFiles = null;
                Common.ShareText = sharedText;

                shareTextBtn.Visibility = ViewStates.Visible;

                bool isValidUri = System.Uri.TryCreate(sharedText, UriKind.Absolute, out _);
                if (isValidUri)
                    shareUrlBtn.Visibility = ViewStates.Visible;
            }
            else
            {
                contentPreview.Text = "Unsupported content";
            }

            shareFileBtn.Click += ShareFileBtn_Click;
            shareUrlBtn.Click += ShareUrlBtn_Click;
            shareTextBtn.Click += ShareTextBtn_Click;

            Analytics.TrackPage("ShareTarget");
        }

        private void ShareFileBtn_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Share_File");
            StartActivity(intent);

            FinishAffinity();
        }

        private void ShareUrlBtn_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Share_Url");
            StartActivity(intent);

            FinishAffinity();
        }

        private void ShareTextBtn_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Share_Text");
            StartActivity(intent);

            FinishAffinity();
        }

        private void OnCreate_Main()
        {
            showToolbarMenu = true;

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            //Toolbar will now take on default actionbar characteristics
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Roamit";

            FindViewById<RelativeLayout>(Resource.Id.main_actions).Visibility = ViewStates.Visible;
            FindViewById<RelativeLayout>(Resource.Id.main_share).Visibility = ViewStates.Gone;

            //FindViewById<Button>(Resource.Id.button3).Click += Button3_Click;
            //FindViewById<Button>(Resource.Id.mainSendMessageCarrier).Click += SendMessageCarrier_Click;
            var mainLayout = FindViewById<LinearLayout>(Resource.Id.mainLayout);
            clipboardButton = FindViewById<Button>(Resource.Id.clipboardButton);
            sendUrlButton = FindViewById<Button>(Resource.Id.main_btn_openUrl);
            sendFileButton = FindViewById<Button>(Resource.Id.sendFileButton);
            sendPictureButton = FindViewById<Button>(Resource.Id.sendPictureButton);
            clipboardPreviewText = FindViewById<TextView>(Resource.Id.clipboardPreviewText);

            clipboardButton.Click += SendClipboard_Click;
            sendUrlButton.Click += SendUrlButton_Click;
            sendFileButton.Click += SendFile_Click;
            sendPictureButton.Click += SendPictureButton_Click;

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

            SetClipboardPreviewText();
            clipboardUpdateTimer = new Timer(ClipboardUpdateTimer_Tick, null, 0, 1000);

            Task.Run(async () =>
            {
#if DEBUG
                FirebaseInstanceId.Instance.DeleteInstanceId();
#endif
                await ServiceFunctions.RegisterDevice();
                RefreshUserTrialStatus();
            });

            Analytics.TrackPage("MainPage");
        }

        private void ClipboardUpdateTimer_Tick(object state)
        {
            SetClipboardPreviewText();
        }

        private void SetClipboardPreviewText()
        {
            string content = ClipboardHelper.GetClipboardText(this);
            RunOnUiThread(() =>
            {
                if (content.Length == 0)
                {
                    clipboardButtonEnabled = false;
                    clipboardButton.Enabled = false;
                    sendUrlButton.Visibility = ViewStates.Gone;

                    clipboardPreviewText.Text = "";
                }
                else
                {
                    string contentPreview = content;

                    //truncate text preview if it's too long
                    if (contentPreview.Length > 61)
                        contentPreview = contentPreview.Substring(0, 60) + "...";

                    // remove newlines
                    contentPreview = contentPreview.Replace("\r", " ").Replace("\n", " ");

                    // remove multiple spaces
                    while (contentPreview.Contains("  "))
                        contentPreview = contentPreview.Replace("  ", " ");

                    clipboardPreviewText.Text = contentPreview;

                    clipboardButtonEnabled = true;
                    if (Common.ListManager.SelectedRemoteSystem != null)
                    {
                        clipboardButton.Enabled = true;

                        bool isValidUri = System.Uri.TryCreate(content, UriKind.Absolute, out _);
                        if (isValidUri)
                        {
                            sendUrlButton.Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            sendUrlButton.Visibility = ViewStates.Gone;
                        }
                    }
                    else
                    {
                        sendUrlButton.Visibility = ViewStates.Gone;
                    }
                }
            });
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (showToolbarMenu)
            {
                MenuInflater.Inflate(Resource.Menu.main, menu);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_settings:
                    var intent = new Intent(this, typeof(SettingsActivity));
                    StartActivity(intent);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        //private void SendMessageCarrier_Click(object sender, EventArgs e)
        //{
        //    StartService(new Intent(this, typeof(MessageCarrierService)));
        //}

        private void SendFile_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "File");
            StartActivity(intent);
        }

        private void SendPictureButton_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Picture");
            StartActivity(intent);
        }

        //WebServerComponent.WebServer web = new WebServerComponent.WebServer();
        //private void InitWebServer()
        //{
        //    var ip = NetworkHelper.GetLocalIPAddress();
        //    Toast.MakeText(this, ip, ToastLength.Long).Show();

        //    web.StartWebServer(ip, 8081);
        //}


        private void SendUrlButton_Click(object sender, EventArgs e)
        {
            SendPageActivity.IsInitialized = false;
            var intent = new Intent(this, typeof(SendPageActivity));
            intent.PutExtra("ContentType", "Url");
            StartActivity(intent);
        }

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
            {
                SetButtonsEnableStatus(false);
                return;
            }

            RunOnUiThread(() =>
            {
                FindViewById<TextView>(Resource.Id.selectedDeviceName).Text = (Common.ListManager.SelectedRemoteSystem?.DisplayName) ?? "";
                System.Diagnostics.Debug.WriteLine(Common.ListManager.SelectedRemoteSystem?.DisplayName ?? "NULL" + " is selected.");

                SetButtonsEnableStatus(true);
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

            var result = await Common.PackageManager.LaunchUri(new System.Uri("http://www.ghiasi.net"), rs);
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
            /*if (!oauthUrl.ToLower().Contains("user.read"))
                oauthUrl = oauthUrl.Replace("&scope=", "&scope=User.Read+");
            if (!oauthUrl.ToLower().Contains("device.read"))
                oauthUrl = oauthUrl.Replace("&scope=", "&scope=Device.Read+");*/

            RunOnUiThread(async () =>
            {
                System.Diagnostics.Debug.WriteLine(oauthUrl);
                var result = await AuthenticateDialog.ShowAsync(this, MsaAuthPurpose.ProjectRomePlatform, oauthUrl);

                if (result != MsaAuthResult.Success)
                {
                    Toast.MakeText(this, "The app can't work without your permission.", ToastLength.Long);
                }
            });
        }
    }
}

