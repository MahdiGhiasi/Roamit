using GoogleAnalytics;
using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.DevicesListManager;
using QuickShare.FileTransfer;
using QuickShare.Classes;
using QuickShare.TextTransfer;
using QuickShare.UWP.Rome;
using QuickShare.ViewModels;
using QuickShare.HelperClasses.Version;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QuickShare.HelperClasses;
using System.Threading;
using PCLStorage;

namespace QuickShare
{
    public sealed partial class MainSend : Page
    {
        public MainSendViewModel ViewModel { get; set; }

        bool sendingFile = false;
        CancellationTokenSource sendFileCancellationTokenSource = new CancellationTokenSource();

        public MainSend()
        {
            this.InitializeComponent();

            ViewModel = new MainSendViewModel()
            {
                SendStatus = "Connecting...",
                ProgressIsIndeterminate = true,
                ProgressValue = 0,
                ProgressMaximum = 0,
                UnlockNoticeVisibility = Visibility.Visible,
                LeaveScreenOnNoticeVisibility = Visibility.Collapsed,
            };
        }

        private void BackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            while (Frame.BackStackDepth > 1)
                Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
            Frame.GoBack();
        }

        public IEnumerable<string> FindMyIPAddresses()
        {
            List<string> ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var hn in hostnames)
            {
                if (hn.Type == Windows.Networking.HostNameType.Ipv4)
                {
                    //IanaInterfaceType == 71 => Wifi
                    //IanaInterfaceType == 6 => Ethernet (Emulator)
                    if (hn.IPInformation != null &&
                    (hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71
                    || hn.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                    {
                        string ipAddress = hn.DisplayName;
                        ipAddresses.Add(ipAddress);
                    }
                }
            }

            return ipAddresses;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (sendingFile)
            {
                sendFileCancellationTokenSource.Cancel();
                e.Cancel = true;
            }
            base.OnNavigatingFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("Send").Build());
#endif
            PersistentDisplay.ActivatePersistentDisplay();

            if (Frame.BackStackDepth > 0)
                if (Frame.BackStack[Frame.BackStackDepth - 1].SourcePageType == typeof(MainSend))
                    Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);

            var rs = MainPage.Current.GetSelectedSystem();

            bool isDestinationAndroid;

            IRomePackageManager packageManager;
            if (rs is NormalizedRemoteSystem)
            {
                isDestinationAndroid = true;
                packageManager = MainPage.Current.AndroidPackageManager;

                var nrs = rs as NormalizedRemoteSystem;
                
                if ((!string.IsNullOrEmpty(nrs.AppVersion)) &&
                    (Version.TryParse(nrs.AppVersion, out Version remoteAppVersion)) &&
                    (remoteAppVersion < new Version("2.1.4")))
                {
                    MainPage.Current.AndroidPackageManager.Mode = AndroidRomePackageManager.AndroidPackageManagerMode.MessageCarrier;
                }
                else
                {
                    PackageManagerHelper.InitAndroidPackageManagerMode();
                }
            }
            else
            {
                isDestinationAndroid = false;
                packageManager = MainPage.Current.PackageManager;
            }

            string deviceName = (new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation()).FriendlyName;
            var mode = e.Parameter.ToString();

            bool succeed = true;
            FileTransferResult fileTransferResult = FileTransferResult.Successful;
            try
            {
                if (mode == "launchUri")
                {
                    await LaunchUri(rs);
                }
                else if (mode == "text")
                {
                    string text = SendDataTemporaryStorage.Text;
                    bool fastSendResult = await TrySendFastClipboard(text, rs, deviceName);

                    if (fastSendResult)
                    {
                        SendTextFinished("");
                    }
                    else
                    {
                        ViewModel.ProgressPercentIndicatorVisibility = Visibility.Visible;
                        RomeAppServiceConnectionStatus result = await Connect(rs);

                        if (result != RomeAppServiceConnectionStatus.Success)
                        {
                            HideEverything();

                            succeed = false;
                            string errorMessage;

                            if ((result == RomeAppServiceConnectionStatus.RemoteSystemUnavailable) && (packageManager is AndroidRomePackageManager))
                            {
                                errorMessage = "Can't connect to device. Open Roamit on your Android device and then try again.";
                            }
                            else
                            {
                                errorMessage = result.ToString();
                            }

                            await (new MessageDialog(errorMessage, "Can't connect")).ShowAsync();
                            Frame.GoBack();
                            return;
                        }

                        ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;

                        await SendText(packageManager, deviceName, text);
                    }
                }
                else if (mode == "file")
                {
                    ViewModel.ProgressPercentIndicatorVisibility = Visibility.Visible;
                    RomeAppServiceConnectionStatus result = await Connect(rs);

                    if (result != RomeAppServiceConnectionStatus.Success)
                    {
                        HideEverything();

                        succeed = false;
                        string errorMessage;

                        if ((result == RomeAppServiceConnectionStatus.RemoteSystemUnavailable) && (packageManager is AndroidRomePackageManager))
                        {
                            errorMessage = "Can't connect to device. Open Roamit on your Android device and then try again.";
                        }
                        else
                        {
                            errorMessage = result.ToString();
                        }

                        await (new MessageDialog(errorMessage, "Can't connect")).ShowAsync();
                        Frame.GoBack();
                        return;
                    }

                    ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;
                    if (!isDestinationAndroid)
                        ViewModel.LeaveScreenOnNoticeVisibility = Visibility.Visible;

                    fileTransferResult = await SendFile(rs, packageManager, deviceName);
                    if (fileTransferResult != FileTransferResult.Successful)
                    {
                        HideEverything();

                        succeed = false;
                        Frame.GoBack();
                        return;
                    }

                    if (!isDestinationAndroid)
                        ViewModel.LeaveScreenOnNoticeVisibility = Visibility.Collapsed;
                }
                else
                {
                    succeed = false;
                    await (new MessageDialog("MainSend::Invalid parameter.")).ShowAsync();
                    Frame.GoBack();
                    return;
                }
            }
            finally
            {
                PersistentDisplay.ReleasePersistentDisplay();
#if !DEBUG
                if (rs is NormalizedRemoteSystem)
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("SendToAndroid", mode, succeed ? "Success" : ((mode == "file") ? fileTransferResult.ToString() : "Failed")).Build());
                else
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("SendToWindows", mode, succeed ? "Success" : ((mode == "file") ? fileTransferResult.ToString() : "Failed")).Build());
#endif
                sendingFile = false;
            }

            if (SendDataTemporaryStorage.IsSharingTarget)
            {
                if (App.ShareOperation == null)
                {
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("Send", "UriTarget").Build());
#endif
                    await Task.Delay(TimeSpan.FromSeconds(1.5));
                    App.Current.Exit();
                }
                else
                {
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("Send", "ShareTarget").Build());
#endif
                    await Task.Delay(TimeSpan.FromSeconds(1.5));
                    App.ShareOperation.ReportCompleted();
                }
                SendDataTemporaryStorage.IsSharingTarget = false;
                App.ShareOperation = null;
            }
            else
            {
                ViewModel.GoBackButtonVisibility = Visibility.Visible;
                GoBackButtonShowStoryboard.Begin();

                if (succeed)
                {
                    await AskForReviewIfNecessary();
                }

#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Send", "WithinApp").Build());
#endif
            }
        }

        private async Task AskForReviewIfNecessary()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("SuccessSendCount"))
                ApplicationData.Current.LocalSettings.Values["SuccessSendCount"] = "0";

            if (!int.TryParse(ApplicationData.Current.LocalSettings.Values["SuccessSendCount"].ToString(), out int count))
                count = 0;

            count++;
            ApplicationData.Current.LocalSettings.Values["SuccessSendCount"] = count.ToString();

            if ((count > 8) && (!ApplicationData.Current.LocalSettings.Values.ContainsKey("AskedForReview")))
            {
                ApplicationData.Current.LocalSettings.Values["AskedForReview"] = "true";

                var dlg = new MessageDialog("We really appreciate it.", "Would you mind rating Roamit in the Store?");
                dlg.Commands.Add(new UICommand
                {
                    Id = 0,
                    Label = "Ok, sure",
                });
                dlg.Commands.Add(new UICommand
                {
                    Id = 1,
                    Label = "No, thanks",
                });
                dlg.DefaultCommandIndex = 0;
                dlg.CancelCommandIndex = 1;

                var result = await dlg.ShowAsync();

                if ((int)result.Id == 0)
                {
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("AskForReview", "Accepted").Build());
#endif
                    ApplicationData.Current.LocalSettings.Values["AskedForReviewResult"] = "accept";
                    await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
                }
                else
                {
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("AskForReview", "Rejected").Build());
#endif
                    ApplicationData.Current.LocalSettings.Values["AskedForReviewResult"] = "reject";
                }
            }
        }

        private async Task<FileTransferResult> SendFile(object rs, IRomePackageManager packageManager, string deviceName)
        {
            string sendingText = ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFile)) ? "Sending file..." : "Sending files...";
            ViewModel.SendStatus = "Preparing...";

            string message = "";
            FileTransferResult result = FileTransferResult.Successful;

            sendingFile = true;

            await DownloadNecessaryFiles();

            using (FileSender fs = new FileSender(rs,
                                                  new QuickShare.UWP.WebServerGenerator(),
                                                  packageManager,
                                                  FindMyIPAddresses(),
                                                  deviceName))
            {
                ViewModel.ProgressMaximum = 1;
                fs.FileTransferProgress += async (ss, ee) =>
                {
                    if (ee.State == FileTransferState.Error)
                    {
                        result = FileTransferResult.FailedOnSend;
                        message = ee.Message;
                    }
                    else
                    {
                        await DispatcherEx.RunOnCoreDispatcherIfPossible(() =>
                        {
                            ViewModel.SendStatus = sendingText;
                            ViewModel.ProgressMaximum = (int)ee.Total + 1;
                            ViewModel.ProgressValue = (int)ee.CurrentPart;
                            ViewModel.ProgressIsIndeterminate = false;
                        }, false);
                    }
                };

                if (SendDataTemporaryStorage.Files.Count == 0)
                {
                    ViewModel.SendStatus = "No files.";
                    ViewModel.ProgressIsIndeterminate = false;
                    return FileTransferResult.NoFiles;
                }
                else if ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFile))
                {
                    await Task.Run(async () =>
                    {
                        result = await fs.SendFile(sendFileCancellationTokenSource.Token, new PCLStorage.WinRTFile(SendDataTemporaryStorage.Files[0] as StorageFile));
                    });
                }
                else if ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFolder))
                {
                    var folder = SendDataTemporaryStorage.Files[0] as StorageFolder;

                    await Task.Run(async () =>
                    {
                        result = await fs.SendQueue(sendFileCancellationTokenSource.Token, await GetFilesOfFolder(folder), folder.DisplayName);
                    });
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        result = await fs.SendFiles(sendFileCancellationTokenSource.Token, 
                            from x in SendDataTemporaryStorage.Files
                            where x is StorageFile
                            select new PCLStorage.WinRTFile(x as StorageFile), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\");
                    });
                }

                ViewModel.ProgressValue = ViewModel.ProgressMaximum;
            }

            sendingFile = false;

            if (result != FileTransferResult.Successful)
            {
                HideEverything();

                if (result != FileTransferResult.Cancelled)
                {
                    string title = "Send failed.";
                    string text = message;
                    if (result == FileTransferResult.FailedOnHandshake)
                    {
                        title = "Couldn't reach remote device.";
                        text = "Make sure both devices are connected to the same Wi-Fi or LAN network.";
                    }

                    var dlg = new MessageDialog(text, title);
                    await dlg.ShowAsync();
                }
            }
            else
            {
                await SendFinishService(packageManager);

                ViewModel.SendStatus = "Finished.";
            }

            return result;
        }

        private async Task DownloadNecessaryFiles()
        {
            foreach (var item in SendDataTemporaryStorage.Files)
            {
                if ((item is StorageFile file) && (!file.IsLocallyAvailable()))
                {
                    //Download it
                    ViewModel.SendStatus = "Retrieving files...";

                    var readStream = await file.OpenStreamForReadAsync();
                    byte[] buffer = new byte[1024 * 1024];
                    while (readStream.Position < readStream.Length)
                    {
                        await readStream.ReadAsync(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        private async Task<List<Tuple<string, IFile>>> GetFilesOfFolder(StorageFolder f, string relPath = "")
        {
            List<Tuple<string, IFile>> files = (from x in await f.GetFilesAsync()
                                                select new Tuple<string, IFile>(relPath + f.Name + "\\", new WinRTFile(x))).ToList();

            var folders = await f.GetFoldersAsync();

            foreach (var folder in folders)
            {
                files.AddRange(await GetFilesOfFolder(folder, relPath + f.Name + "\\"));
            }

            return files;
        }

        private void HideEverything()
        {
            ViewModel.SendStatus = "";
            ViewModel.ProgressIsIndeterminate = false;
            ViewModel.ProgressValue = 0;
            ViewModel.ProgressMaximum = 100;
            ViewModel.ProgressPercentIndicatorVisibility = Visibility.Collapsed;
            ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;
            ViewModel.LeaveScreenOnNoticeVisibility = Visibility.Collapsed;
        }

        private async Task SendText(IRomePackageManager packageManager, string deviceName, string text)
        {
            ViewModel.ProgressPercentIndicatorVisibility = Visibility.Collapsed;
            TextSender ts = new TextSender(packageManager, deviceName);

            ts.TextSendProgress += (ee) =>
            {
                ViewModel.ProgressIsIndeterminate = false;
                ViewModel.ProgressMaximum = ee.TotalParts;
                ViewModel.ProgressValue = ee.SentParts;
            };

            ViewModel.SendStatus = "Sending...";

            bool sendResult = await ts.Send(text, ContentType.ClipboardContent);

            if (sendResult)
            {
                await SendFinishService(packageManager);
                ViewModel.SendStatus = "Finished.";
            }
            else
            {
                ViewModel.SendStatus = "Failed :(";
            }

            ViewModel.ProgressIsIndeterminate = false;
            ViewModel.ProgressValue = ViewModel.ProgressMaximum;
        }

        private async Task LaunchUri(object rs)
        {
            ViewModel.ProgressPercentIndicatorVisibility = Visibility.Collapsed;

            RomeRemoteLaunchUriStatus launchResult;

            if (rs is NormalizedRemoteSystem)
                launchResult = await UWP.Rome.AndroidRomePackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri, rs as NormalizedRemoteSystem, SecureKeyStorage.GetUserId());
            else
                launchResult = await MainPage.Current.PackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri, rs);

            string status;
            if (launchResult == RomeRemoteLaunchUriStatus.Success)
                status = "";
            else
                status = launchResult.ToString();
            SendTextFinished(status);
        }

        private void SendTextFinished(string errorMessage)
        {
            ViewModel.SendStatus = (errorMessage.Length == 0) ? "Finished." : errorMessage;
            ViewModel.ProgressIsIndeterminate = false;
            ViewModel.ProgressMaximum = 100;
            ViewModel.ProgressValue = ViewModel.ProgressMaximum;
            ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;
        }

        private static async Task SendFinishService(IRomePackageManager packageManager)
        {
            Dictionary<string, object> vs = new Dictionary<string, object>
                    {
                        { "Receiver", "System" },
                        { "FinishService", "FinishService" }
                    };
            await packageManager.Send(vs);
        }

        private async Task<RomeAppServiceConnectionStatus> Connect(object rs)
        {
            if (rs is NormalizedRemoteSystem)
                return await MainPage.Current.AndroidPackageManager.Connect(rs as NormalizedRemoteSystem,
                    SecureKeyStorage.GetUserId(),
                    MainPage.Current.PackageManager.RemoteSystems.Where(x => x.Kind != "Unknown").Select(x => x.Id));
            else
                return await MainPage.Current.PackageManager.Connect(rs, true, new Uri("roamit://wake"));
        }

        private async Task<bool> TrySendFastClipboard(string text, object rs, string deviceName)
        {
            if (rs is NormalizedRemoteSystem)
                return await AndroidRomePackageManager.QuickClipboard(text,
                    rs as NormalizedRemoteSystem,
                    SecureKeyStorage.GetUserId(),
                    deviceName);
            else
                return await MainPage.Current.PackageManager.QuickClipboard(text,
                    rs as RemoteSystem,
                    deviceName,
                    "roamit://clipboard");
        }
    }
}
