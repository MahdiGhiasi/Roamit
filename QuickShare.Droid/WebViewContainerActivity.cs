using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using Microsoft.ConnectedDevices;
using QuickShare.Droid.OnlineServiceHelpers;
using System.Threading;
using QuickShare.DevicesListManager;
using QuickShare.Droid.RomeComponent;
using Android.Database;
using Android.Provider;
using QuickShare.Droid.Classes;
using System.Threading.Tasks;
using Com.Github.Angads25.Filepicker.Model;
using Com.Github.Angads25.Filepicker.View;
using QuickShare.Common;
using QuickShare.FileTransfer;
using Plugin.DeviceInfo;
using QuickShare.TextTransfer;
using Firebase.Iid;
using QuickShare.Droid.Classes.RevMob;
using Com.Revmob.Ads.Banner;
using Com.Revmob;

namespace QuickShare.Droid
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.webviewcontainerpage")]
    public class WebViewContainerActivity : Activity
    {
        readonly string homeUrl = "file:///android_asset/html/home.html";

        bool automaticRemoteSystemSelectionAllowed = true;
        int remoteSystemPrevCount = 0;

        WebView webView;

        SemaphoreSlim rsChangeSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim jsSendSemaphore = new SemaphoreSlim(1, 1);
        static bool IsInitialized = false;

        System.Timers.Timer finishLoadingTimer = null, checkClipboardTextTimer = null;
        Object finishLoadingLock = new Object();

        internal static CallbackStartSessionListener startSessionListener;
        internal static CallbackShowBanner showBannerAdListener;
        RevMobBanner revmobBanner;
        LinearLayout devicesListLayout;
        RelativeLayout bannerLayout;

        bool sendingFile = true;
        CancellationTokenSource sendFileCancellationTokenSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.WebViewContainer);

            webView = FindViewById<WebView>(Resource.Id.webView);
            var client = new HybridWebViewClient(this);
            webView.SetWebViewClient(client);
            webView.Settings.JavaScriptEnabled = true;

            if (IsShareDialog)
            {
                InitShareDialog();
            }
            else
            {
                webView.LoadUrl(homeUrl);

                ShowWhatsNewIfNecessary();
            }


            bannerLayout = FindViewById<RelativeLayout>(Resource.Id.webViewContainer_banner);
            startSessionListener = new CallbackStartSessionListener(this);
            showBannerAdListener = new CallbackShowBanner(this);
            RevMob.StartWithListener(this, startSessionListener, Droid.Config.Secrets.RevMobId);
            UserTrialStatusUpdated();
            TrialHelper.UserTrialStatusChanged += UserTrialStatusUpdated;
            RefreshUserTrialStatus();


            checkClipboardTextTimer = new System.Timers.Timer()
            {
                Interval = 1000,
            };
            checkClipboardTextTimer.Elapsed += CheckClipboardTextTimer_Elapsed;
            checkClipboardTextTimer.Start();

            if (IsInitialized)
            {
                //TODO: Load already available devices to UI
                Common.PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;
                return;
            }
            IsInitialized = true;

            if (Common.PackageManager == null)
            {
                Common.PackageManager = new RomePackageManager(this);
                Common.PackageManager.Initialize("com.roamit.service");
            }
            else
            {
                foreach (var item in Common.PackageManager.RemoteSystems)
                {
                    Common.ListManager.AddDevice(item);
                }
            }
            //TODO: else: Load already available devices to UI

            if (Common.MessageCarrierPackageManager == null)
            {
                Common.MessageCarrierPackageManager = new RomePackageManager(this);
                Common.MessageCarrierPackageManager.Initialize("com.roamit.messagecarrierservice");
            }

            InitDiscovery();

            Task.Run(async () =>
            {
#if DEBUG
                FirebaseInstanceId.Instance.DeleteInstanceId();
#endif
                await ServiceFunctions.RegisterDevice();
                RefreshUserTrialStatus();
            });

            Analytics.TrackPage("WebViewContainerActivity");

            var settings = new Classes.Settings(this);
            if (settings.AllowToStayInBackground)
                StartService(new Intent(this, typeof(Services.RomeReadyService)));

            CheckForLegacyVersionInstallations();
        }

        private void ShowWhatsNewIfNecessary()
        {
            WhatsNew whatsNew = new WhatsNew(this);

            if (!whatsNew.ShouldShowWhatsNew)
                return;

            var alert = new AlertDialog.Builder(this)
                    .SetTitle(whatsNew.GetTitle())
                    .SetMessage(whatsNew.GetText())
                    .SetPositiveButton("Got it", (s, e) => { });

            RunOnUiThread(() =>
            {
                alert.Show();
            });

            whatsNew.Shown();
        }

        private async void CheckForLegacyVersionInstallations()
        {
            bool installed = PackageManager.GetInstalledPackages(Android.Content.PM.PackageInfoFlags.MatchAll).FirstOrDefault(x => x.PackageName == "com.ghiasi.roamit") != null;

            if (installed)
            {
                while ((Common.ListManager.RemoteSystems.Count == 0) && (Common.ListManager.SelectedRemoteSystem == null))
                    await Task.Delay(500);

                var alert = new AlertDialog.Builder(this)
                    .SetTitle("Please uninstall the legacy version of Roamit.")
                    .SetMessage("Having both versions side-by-side might cause problems.\n" +
                    "Uninstall the previous version to have the best experience possible.")
                    .SetPositiveButton("OK", (s,e) => { });

                RunOnUiThread(() =>
                {
                    alert.Show();
                });
            }
        }

        private async void UserTrialStatusUpdated()
        {
            if (TrialHelper.UserTrialStatus == QuickShare.Common.Service.UpgradeDetails.VersionStatus.TrialVersion)
                await ShowBanner();
            else
                HideBanner();
        }

        private void HideBanner()
        {
            bannerLayout.Visibility = ViewStates.Gone;

            if (revmobBanner != null)
                revmobBanner.Hide();
        }

        private async Task ShowBanner()
        {
            try
            {
                bannerLayout.Visibility = ViewStates.Visible;

                if (revmobBanner != null)
                {
                    revmobBanner.Show();
                    return;
                }

                var revMob = await RevMobHelper.TryGetAdMobSessionAsync(startSessionListener);
                if (revMob == null)
                {
                    HideBanner();
                    return;
                }

                revmobBanner = revMob.CreateBanner(this, "", showBannerAdListener);
                revmobBanner.SetAutoShow(true);

                ViewGroup view = FindViewById<ViewGroup>(Resource.Id.webViewContainer_banner);

                view.RemoveAllViews();
                view.AddView(revmobBanner);
            }
            catch { }
        }

        private bool IsShareDialog { get => ((Intent.Action == Intent.ActionSend) || (Intent.Action == Intent.ActionSendMultiple)); }

        private void InitShareDialog()
        {
            if ((Intent.Action == Intent.ActionSend) && (Intent.Extras.ContainsKey(Intent.ExtraStream)))
            {
                var fileUrl = FilePathHelper.GetPath(this, (Android.Net.Uri)Intent.Extras.GetParcelable(Intent.ExtraStream));

                Common.ShareFiles = new string[] { fileUrl };

                webView.LoadUrl($"{homeUrl}#sharefile");
            }
            else if (Intent.Action == Intent.ActionSendMultiple && Intent.Extras.ContainsKey(Intent.ExtraStream))
            {
                string[] urls = Intent.Extras.GetParcelableArrayList(Intent.ExtraStream)
                    .Cast<Android.Net.Uri>()
                    .Select(x => FilePathHelper.GetPath(this, x))
                    .ToArray();

                Common.ShareFiles = urls;

                webView.LoadUrl($"{homeUrl}#sharefile");
            }
            else if ((Intent.Action == Intent.ActionSend) && (Intent.Type == "text/plain"))
            {
                string sharedText = Intent.GetStringExtra(Intent.ExtraText);

                Common.ShareFiles = null;
                Common.ShareText = sharedText;

                bool isValidUri = System.Uri.TryCreate(sharedText, UriKind.Absolute, out _);
                if (isValidUri)
                    webView.LoadUrl($"{homeUrl}#sharelink");
                else
                    webView.LoadUrl($"{homeUrl}#shareclipboard");
            }
            else
            {
                webView.LoadUrl($"{homeUrl}#share");
            }
        }

        private async void RefreshUserTrialStatus()
        {
            if (MSAAuthenticator.HasUserUniqueId())
            {
                var userId = await MSAAuthenticator.GetUserUniqueIdAsync();
                await TrialHelper.RefreshUserTrialStatusAsync(userId);
            }
        }


        private void CheckClipboardTextTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SetClipboardPreviewText();
        }

        private void WebViewBack()
        {
            SendJavascriptToWebView("window.history.back();");
        }

        public override void OnBackPressed()
        {
            if ((webView.Url != homeUrl) && (!IsShareDialog))
            {
                if (sendingFile)
                {
                    if (sendFileCancellationTokenSource == null)
                        sendingFile = false;
                    else
                        sendFileCancellationTokenSource.Cancel();
                }

                WebViewBack();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private async void SendJavascriptToWebView(string jsContent)
        {
            await jsSendSemaphore.WaitAsync();
            try
            {
                webView.EvaluateJavascript(jsContent, null);
                await Task.Delay(20);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"*** ERROR IN SendJavascriptToWebView ({jsContent}): {ex.Message}. Will try loadUrl instead.");

                try
                {
                    webView.LoadUrl($"javascript:{jsContent}");
                    await Task.Delay(20);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"*** ERROR #2 IN SendJavascriptToWebView ({jsContent}): {ex2.Message}. Sending javascript command failed.");
                }
            }
            finally
            {
                jsSendSemaphore.Release();
            }
        }

        private async void InitDiscovery()
        {
            Common.PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;
            Platform.FetchAuthCode += Platform_FetchAuthCode;

            await Common.PackageManager.InitializeDiscovery();
            await Common.MessageCarrierPackageManager.InitializeDiscovery();
        }

        private async void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await rsChangeSemaphore.WaitAsync();

            try
            {
                RunOnUiThread(() =>
                {
                    var normalizer = new RemoteSystemNormalizer();
                    if (e.NewItems != null)
                        foreach (var item in e.NewItems)
                        {
                            Common.ListManager.AddDevice(item);
                            var nrs = normalizer.Normalize(item);
                            AddRemoteSystemToList(nrs);
                        }

                    if (e.OldItems != null)
                        foreach (var item in e.OldItems)
                        {
                            Common.ListManager.RemoveDevice(item);
                            var nrs = normalizer.Normalize(item);
                            RemoveRemoteSystemFromList(nrs);
                        }

                    SelectItemIfNecessary();

                    if ((Common.ListManager.RemoteSystems.Count > 0) || (Common.ListManager.SelectedRemoteSystem != null))
                    {
                        AuthenticateDialog.Hide();
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

                            finishLoadingTimer.Start();
                            System.Diagnostics.Debug.WriteLine("Timer started...");
                        }
                    }
                    catch { }

                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                rsChangeSemaphore.Release();
            }
        }

        private void RemoveRemoteSystemFromList(NormalizedRemoteSystem nrs)
        {
            SendJavascriptToWebView($"removeItem('{nrs.Id.NormalizeForJsCall()}');");
        }

        private void AddRemoteSystemToList(NormalizedRemoteSystem nrs)
        {
            SendJavascriptToWebView($"addItem('{nrs.DisplayName.NormalizeForJsCall()}', '{TranslateDeviceKindToWebViewFormat(nrs.Kind).NormalizeForJsCall()}', '{nrs.Id.NormalizeForJsCall()}');");
        }

        private void SelectItemIfNecessary()
        {
            if ((Common.ListManager.RemoteSystems.Count > 0) &&
                (((automaticRemoteSystemSelectionAllowed) /*&& (Common.ListManager.RemoteSystems.Count > remoteSystemPrevCount)*/) || (Common.ListManager.SelectedRemoteSystem == null)))
            {
                remoteSystemPrevCount = Common.ListManager.RemoteSystems.Count;
                Common.ListManager.SelectHighScoreItem();
                var s = $"selectItem('{Common.ListManager.SelectedRemoteSystem?.Id?.NormalizeForJsCall()}');";
                SendJavascriptToWebView(s);

                BlockAutomaticRemoteSystemSelection();
            }
        }

        private async void BlockAutomaticRemoteSystemSelection()
        {
            if (!automaticRemoteSystemSelectionAllowed)
                return;

            await Task.Delay(TimeSpan.FromSeconds(1));
            automaticRemoteSystemSelectionAllowed = false;
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

        public string TranslateDeviceKindToWebViewFormat(string kind)
        {
            switch (kind.ToLower())
            {
                case "xbox":
                    return "videogame_asset";
                case "mobile":
                case "phone":
                    return "smartphone";
                case "unknown":
                default:
                    return "laptop";
            }
        }

        private void Platform_FetchAuthCode(string oauthUrl)
        {
            RunOnUiThread(async () =>
            {
                System.Diagnostics.Debug.WriteLine(oauthUrl);
                var result = await AuthenticateDialog.ShowAsync(this, MsaAuthPurpose.ProjectRomePlatform, oauthUrl);

                if ((result != MsaAuthResult.Success) && (result != MsaAuthResult.CancelledByApp))
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
            });
        }

        private void SetClipboardPreviewText()
        {
            try
            {
                RunOnUiThread(() =>
                {
                    string content = ClipboardHelper.GetClipboardText(this);

                    var contentPreview = content;

                    //truncate text preview if it's too long
                    if (contentPreview.Length > 61)
                        contentPreview = contentPreview.Substring(0, 60) + "...";

                    // remove newlines
                    contentPreview = contentPreview.Replace("\r", " ").Replace("\n", " ");

                    // remove multiple spaces
                    while (contentPreview.Contains("  "))
                        contentPreview = contentPreview.Replace("  ", " ");

                    SendJavascriptToWebView($"setClipboardText('{contentPreview.NormalizeForJsCall()}');");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Can't read clipboard: " + ex.Message);
            }
        }

        private async void SelectDevice(string id)
        {
            if ((Common.ListManager.SelectedRemoteSystem != null) && (Common.ListManager.SelectedRemoteSystem.Id == id))
                return;

            var item = Common.ListManager.RemoteSystems.FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                await Task.Delay(1000);

                item = Common.ListManager.RemoteSystems.FirstOrDefault(x => x.Id == id);
                if (item == null)
                {
                    Common.ListManager.SelectedRemoteSystem = null;
                    SelectItemIfNecessary();

                    return;
                }
            }

            Common.ListManager.Select(item);
        }

        #region Send
        public static readonly int PickImageId = 1000;

        private void ShowProgress()
        {
            SendJavascriptToWebView("showProgress();");
            Analytics.TrackPage("Send");
        }

        private void SetProgressText(string text)
        {
            SendJavascriptToWebView($"setProgressText('{text.NormalizeForJsCall()}');");
        }

        private void SetProgressValue(double val, double max)
        {
            var d = val / max;
            SendJavascriptToWebView($"setProgressValue({d});");
        }

        private void SetProgressValueToIndetermine()
        {
            SetProgressValue(-1.0, 1.0);
        }

        private string GetRealPathFromURI(Android.Net.Uri contentURI)
        {
            ICursor cursor = ContentResolver.Query(contentURI, null, null, null, null);
            cursor.MoveToFirst();
            string documentId = cursor.GetString(0);
            documentId = documentId.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new[] { documentId }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == PickImageId)
            {
                if ((resultCode == Result.Ok) && (data != null))
                {
                    List<string> files = new List<string>();

                    ClipData clipData = data.ClipData;
                    if (clipData != null)
                    {
                        for (int i = 0; i < clipData.ItemCount; i++)
                        {
                            ClipData.Item item = clipData.GetItemAt(i);
                            var uri = item.Uri;
                            files.Add(FilePathHelper.GetPath(this, uri));
                        }
                    }
                    else
                    {
                        Android.Net.Uri uri = data.Data;
                        var file = FilePathHelper.GetPath(this, uri);
                        files.Add(file);
                    }

                    if (files.Count == 0)
                    {
                        return;
                    }

                    await SendFiles(files.ToArray());
                }
                else
                {

                }
            }
        }

        private void PickAndSendPicture()
        {
            Intent getIntent = new Intent(Intent.ActionGetContent);
            getIntent.SetType("image/*");
            getIntent.PutExtra(Intent.ExtraAllowMultiple, true);
            getIntent.PutExtra(Intent.ExtraLocalOnly, true);

            Intent pickIntent = new Intent(Intent.ActionPick, Android.Provider.MediaStore.Images.Media.ExternalContentUri);
            pickIntent.SetType("image/*");
            pickIntent.PutExtra(Intent.ExtraAllowMultiple, true);
            pickIntent.PutExtra(Intent.ExtraLocalOnly, true);

            Intent chooserIntent = Intent.CreateChooser(getIntent, "Select Picture");
            chooserIntent.PutExtra(Intent.ExtraInitialIntents, new Intent[] { pickIntent });

            StartActivityForResult(chooserIntent, PickImageId);
        }

        private async Task PickAndSendFile()
        {
            //TODO: check permission.

            var filePickerTask = new TaskCompletionSource<string[]>();

            DialogProperties properties = new DialogProperties()
            {
                SelectionMode = DialogConfigs.MultiMode,
                SelectionType = DialogConfigs.FileSelect,
                Root = new Java.IO.File(DialogConfigs.DefaultDir),
                ErrorDir = new Java.IO.File(DialogConfigs.DefaultDir),
                Offset = new Java.IO.File(DialogConfigs.DefaultDir),
                Extensions = null
            };

            FilePickerDialog dialog = new FilePickerDialog(this, properties);
            dialog.SetTitle("Select files");
            dialog.DialogSelection += (ss, ee) =>
            {
                filePickerTask.TrySetResult(ee.P0);
            };
            dialog.DismissEvent += (ss, ee) =>
            {
                filePickerTask.TrySetResult(new string[] { });
            };
            dialog.Show();

            string[] files = await filePickerTask.Task;

            if (files.Length == 0)
            {
                return;
            }

            await SendFiles(files);
        }

        private async Task SendFiles(string[] files)
        {
            if (TrialHelper.UserTrialStatus == QuickShare.Common.Service.UpgradeDetails.VersionStatus.TrialVersion)
            {
                double totalSize = 0;
                foreach (var item in files)
                {
                    var info = new System.IO.FileInfo(item);
                    totalSize += info.Length;
                }
                totalSize /= 1024.0 * 1024.0;

                if (totalSize > Constants.MaxSizeForTrialVersion)
                {
                    var intent = new Intent(this, typeof(MessageShowActivity));
                    intent.PutExtra("message", "trialNotice");
                    StartActivity(intent);
                    return;
                }
            }

            ShowProgress();
            SetProgressText("Connecting...");

            var receiveDialogOpenResult = await Common.PackageManager.LaunchUri(new Uri("roamit://receiveDialog"), Common.GetCurrentRemoteSystem());
            if (receiveDialogOpenResult != QuickShare.Common.Rome.RomeRemoteLaunchUriStatus.Success)
            {
                Analytics.TrackEvent("SendToWindows", "file", "Failed");
                SetProgressText($"Communication failed. ({receiveDialogOpenResult.ToString()})");
                return;
            }

            var result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                Analytics.TrackEvent("SendToWindows", "file", "Failed");
                SetProgressText($"Connect failed. ({result.ToString()})");
                return;
            }

            //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
            Common.PackageManager.CloseAppService();
            result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                Analytics.TrackEvent("SendToWindows", "file", "Failed");
                SetProgressText($"Connect failed. ({result.ToString()})");
                return;
            }

            string sendingText = (files.Length == 1) ? "Sending file..." : "Sending files...";
            SetProgressText("Preparing...");

            string message = "";
            FileTransferResult transferResult = FileTransferResult.Successful;

            using (FileSender fs = new FileSender(Common.GetCurrentRemoteSystem(),
                                                  new WebServerComponent.WebServerGenerator(),
                                                  Common.PackageManager,
                                                  FindMyIPAddresses(),
                                                  CrossDeviceInfo.Current.Model))
            {
                fs.FileTransferProgress += (ss, ee) =>
                {
                    if (ee.State == FileTransferState.Error)
                    {
                        transferResult = FileTransferResult.FailedOnSend;
                        message = ee.Message;
                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {
                            SetProgressText(sendingText);

                            SetProgressValue((double)ee.CurrentPart, (double)ee.Total + 1);
                        });
                    }
                };

                sendFileCancellationTokenSource = new CancellationTokenSource();

                if (files.Length == 0)
                {
                    SetProgressText("No files.");
                    //ViewModel.ProgressIsIndeterminate = false;
                    return;
                }
                else if (files.Length == 1)
                {
                    sendingFile = true;

                    await Task.Run(async () =>
                    {
                        transferResult = await fs.SendFile(sendFileCancellationTokenSource.Token, new PCLStorage.FileSystemFile(files[0]));
                    });
                    
                    sendingFile = false;
                }
                else
                {
                    sendingFile = true;
                    await Task.Run(async () =>
                    {
                        transferResult = await fs.SendFiles(sendFileCancellationTokenSource.Token,
                            from x in files
                            select new PCLStorage.FileSystemFile(x), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\");
                    });
                    sendingFile = false;
                }

                sendFileCancellationTokenSource = null;
            }

            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "System" },
                { "FinishService", "FinishService" },
            };
            await Common.PackageManager.Send(vs);

            Common.PackageManager.CloseAppService();

            SetProgressValue(1.0, 1.0);

            if (transferResult != FileTransferResult.Successful)
            {
                Analytics.TrackEvent("SendToWindows", "file", transferResult.ToString());

                if (transferResult != FileTransferResult.Cancelled)
                {
                    SetProgressText("Failed.");
                    SetProgressValue(0, 1.0);
                    System.Diagnostics.Debug.WriteLine("Send failed.\r\n\r\n" + message);

                    if (transferResult == FileTransferResult.FailedOnHandshake)
                    {
                        message = "Couldn't reach remote device.\r\n\r\n" +
                            "Make sure both devices are connected to the same Wi-Fi or LAN network.";
                    }

                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                    alert.SetTitle(message);
                    alert.SetPositiveButton("OK", (senderAlert, args) => { });
                    RunOnUiThread(() =>
                    {
                        alert.Show();
                    });
                }
            }
            else
            {
                SetProgressText("Finished.");
                Analytics.TrackEvent("SendToWindows", "file", "Success");
            }
        }

        private IEnumerable<string> FindMyIPAddresses()
        {
            return NetworkHelper.GetLocalIPAddresses();
        }

        private async Task OpenUrl(string url)
        {
            ShowProgress();
            SetProgressText("Connecting...");

            var result = await Common.PackageManager.LaunchUri(new Uri(url), Common.GetCurrentRemoteSystem());

            SetProgressValue(1.0, 1.0);
            if (result == QuickShare.Common.Rome.RomeRemoteLaunchUriStatus.Success)
            {
                SetProgressText("Finished.");

                Analytics.TrackEvent("SendToWindows", "launchUri", "Success");
            }
            else
            {
                SetProgressText(result.ToString());
                Analytics.TrackEvent("SendToWindows", "launchUri", result.ToString());
            }
        }

        private async Task SendText(string text)
        {
            ShowProgress();
            SetProgressText("Connecting...");

            if (!(await Common.PackageManager.QuickClipboard(text, Common.GetCurrentRemoteSystem(), CrossDeviceInfo.Current.Model, "roamit://clipboard")))
            {

                var result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

                if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
                {
                    SetProgressText($"Connect failed. ({result.ToString()})");
                    Analytics.TrackEvent("SendToWindows", "text", "Failed");
                    return;
                }

                //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
                Common.PackageManager.CloseAppService();
                result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

                if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
                {
                    SetProgressText($"Connect failed. ({result.ToString()})");
                    Analytics.TrackEvent("SendToWindows", "text", result.ToString());
                    return;
                }

                TextSender textSender = new TextSender(Common.PackageManager, CrossDeviceInfo.Current.Model);

                textSender.TextSendProgress += (ee) =>
                {
                    SetProgressValue((double)ee.SentParts, (double)ee.TotalParts);
                };

                SetProgressText("Sending...");

                bool sendResult = await textSender.Send(text, ContentType.ClipboardContent);

                SetProgressValue(1.0, 1.0);

                if (!sendResult)
                {
                    SetProgressText("Send failed.");
                    SetProgressValue(1.0, 1.0);
                    Analytics.TrackEvent("SendToWindows", "text", "Failed");
                    return;
                }

                Common.PackageManager.CloseAppService();
            }

            SetProgressText("Finished.");
            SetProgressValue(1.0, 1.0);
            Analytics.TrackEvent("SendToWindows", "text", "Success");
        }
        #endregion

        class HybridWebViewClient : WebViewClient
        {
            WebViewContainerActivity context;
            public HybridWebViewClient(WebViewContainerActivity _context)
            {
                context = _context;
            }

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.Contains("#progress"))
                    return;

                if ((context.Intent.Action == Intent.ActionSend) || (context.Intent.Action == Intent.ActionSendMultiple))
                {
                    string previewText = "Unsupported content.";

                    if (Common.ShareFiles != null)
                    {
                        if (Common.ShareFiles.Length == 1)
                            previewText = Common.ShareFiles[0];
                        else
                            previewText = $"{Common.ShareFiles.Length} files";
                    }
                    else if (Common.ShareText.Length > 0)
                    {
                        previewText = Common.ShareText;
                    }

                    context.SendJavascriptToWebView($"setSharePreview('{previewText.NormalizeForJsCall()}');");
                }

                if ((Common.ListManager != null) && (Common.ListManager.RemoteSystems != null) && (Common.ListManager.RemoteSystems.Count > 0))
                {
                    foreach (var item in Common.ListManager.RemoteSystems)
                        context.AddRemoteSystemToList(item);

                    if (Common.ListManager.SelectedRemoteSystem == null)
                    {
                        context.SelectItemIfNecessary();
                    }
                    else
                    {
                        context.AddRemoteSystemToList(Common.ListManager.SelectedRemoteSystem);
                        context.automaticRemoteSystemSelectionAllowed = false;

                        var s = $"selectItem('{Common.ListManager.SelectedRemoteSystem?.Id?.NormalizeForJsCall()}');";
                        context.SendJavascriptToWebView(s);
                    }
                }

                base.OnPageFinished(view, url);
            }

            [Obsolete]
            public override bool ShouldOverrideUrlLoading(WebView webView, string url)
            {
                System.Diagnostics.Debug.WriteLine($"** {url}");
                if (url == "file:///android_asset/html/settings.html")
                {
                    var intent = new Intent(context, typeof(SettingsActivity));
                    context.StartActivity(intent);
                }
                else if (url == "file:///android_asset/html/stopAutomaticSelection.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                }
                else if (url == "file:///android_asset/html/sendFile.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("File");
                }
                else if (url == "file:///android_asset/html/sendPhoto.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Photo");
                }
                else if (url == "file:///android_asset/html/sendClipboard.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Clipboard");
                }
                else if (url == "file:///android_asset/html/sendUrl.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Url");
                }
                else if (url.Contains("file:///android_asset/html/selectItem.html"))
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    var id = url.Split('?')[1];
                    context.SelectDevice(id);
                }
                else if (url == "file:///android_asset/html/shareFile.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Share_File");
                }
                else if (url == "file:///android_asset/html/shareClipboard.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Share_Text");
                }
                else if (url == "file:///android_asset/html/shareLink.html")
                {
                    context.automaticRemoteSystemSelectionAllowed = false;
                    ProcessRequest("Share_Url");
                }
                else if (url == "file:///android_asset/html/rate.html")
                {
                    var uri = Android.Net.Uri.Parse("market://details?id=" + Application.Context.PackageName);
                    Intent rateAppIntent = new Intent(Intent.ActionView, uri);

                    if (context.PackageManager.QueryIntentActivities(rateAppIntent, 0).Count > 0)
                    {
                        context.StartActivity(rateAppIntent);
                    }
                }
                else if (url == "file:///android_asset/html/contact.html")
                {
                    var mailto = "mailto:roamitapp@gmail.com?subject=Roamit%20for%20Android%20v" + Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;

                    var email = new Intent(Intent.ActionSendto);
                    email.SetData(Android.Net.Uri.Parse(mailto));

                    if (context.PackageManager.QueryIntentActivities(email, 0).Count > 0)
                    {
                        context.StartActivity(email);
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }

            private async void ProcessRequest(string contentType)
            {
                bool isFromShareTarget = false;

                switch (contentType)
                {
                    case "Clipboard":
                        await context.SendText(ClipboardHelper.GetClipboardText(context));
                        break;
                    case "Photo":
                        context.PickAndSendPicture();
                        break;
                    case "File":
                        await context.PickAndSendFile();
                        break;
                    case "Url":
                        await context.OpenUrl(ClipboardHelper.GetClipboardText(context));
                        break;
                    case "Share_File":
                        isFromShareTarget = true;
                        await context.SendFiles(Common.ShareFiles);
                        break;
                    case "Share_Url":
                        isFromShareTarget = true;
                        await context.OpenUrl(Common.ShareText);
                        break;
                    case "Share_Text":
                        isFromShareTarget = true;
                        await context.SendText(Common.ShareText);
                        break;
                    case "Unknown":
                    default:
                        Analytics.TrackException($"SendPageActivity: Unknown contentType '{contentType}'.", false);
                        break;
                }

                if (isFromShareTarget)
                    Analytics.TrackEvent("Send", "ShareTarget");
                else
                    Analytics.TrackEvent("Send", "WithinApp");
            }

        }
    }
}