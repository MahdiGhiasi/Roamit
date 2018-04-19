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
using Newtonsoft.Json;

namespace QuickShare
{
    public sealed partial class MainSend : Page
    {
        public MainSendViewModel ViewModel { get; set; }

        bool sendingFile = false;
        CancellationTokenSource sendFileCancellationTokenSource = new CancellationTokenSource(), rertieveCancellationTokenSource = new CancellationTokenSource();
        bool retrievingfiles = false;


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
            }

            if (retrievingfiles)
            {
                rertieveCancellationTokenSource.Cancel();
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

            bool preserveFolderStructure = (Frame.BackStackDepth <= 0 || Frame.BackStack[Frame.BackStackDepth - 1].SourcePageType != typeof(PicturePicker));

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

                            Frame.Navigate(typeof(MainSendFailed), JsonConvert.SerializeObject(new SendFailedViewModel
                            {
                                ErrorTitle = "Can't connect",
                                ErrorDescription = errorMessage,
                            }));
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

                        Frame.Navigate(typeof(MainSendFailed), JsonConvert.SerializeObject(new SendFailedViewModel
                        {
                            ErrorTitle = "Can't connect",
                            ErrorDescription = errorMessage,
                        }));
                        return;
                    }

                    ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;
                    if (!isDestinationAndroid)
                        ViewModel.LeaveScreenOnNoticeVisibility = Visibility.Visible;

                    fileTransferResult = await SendFile(rs, packageManager, deviceName, preserveFolderStructure);
                    if (fileTransferResult != FileTransferResult.Successful)
                    {
                        succeed = false;
                        return;
                    }

                    if (!isDestinationAndroid)
                        ViewModel.LeaveScreenOnNoticeVisibility = Visibility.Collapsed;
                }
                else
                {
                    succeed = false;
                    Frame.Navigate(typeof(MainSendFailed), JsonConvert.SerializeObject(new SendFailedViewModel
                    {
                        ErrorTitle = "Invalid parameter",
                        ErrorDescription = "[MainSend]",
                    }));
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

        private async Task<FileTransferResult> SendFile(object rs, IRomePackageManager packageManager, string deviceName, bool preserveFolderStructure)
        {
            string sendingText = ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFile)) ? "Sending file..." : "Sending files...";

            string message = "";
            FileTransferResult result = FileTransferResult.Successful;

            if (await DownloadNecessaryFiles() == false)
            {
                return FileTransferResult.Cancelled;
            }

            sendingFile = true;

            ViewModel.SendStatus = "Preparing...";

            using (FileSender2 fs = new FileSender2(rs,
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
                        message = result.ToString(); // TODO
                    }
                    else
                    {
                        await DispatcherEx.RunOnCoreDispatcherIfPossible(() =>
                        {
                            ViewModel.SendStatus = sendingText;
                            ViewModel.ProgressMaximum = 1.0;
                            ViewModel.ProgressValue = ee.Progress;
                            ViewModel.ProgressIsIndeterminate = false;

                            if (ee.TotalTransferredBytes > 0)
                                ViewModel.ProgressCaption = StringFunctions.GetSizeString(ee.TotalTransferredBytes);
                        }, false);
                    }
                };

                if (SendDataTemporaryStorage.Files.Count == 0)
                {
                    ViewModel.SendStatus = "No files.";
                    ViewModel.ProgressIsIndeterminate = false;
                    return FileTransferResult.NoFiles;
                }
                
                await Task.Run(async () =>
                {
                    result = await fs.Send(preserveFolderStructure ? await GetFiles(SendDataTemporaryStorage.Files) : await GetFilesWithoutFolderStructure(SendDataTemporaryStorage.Files), 
                        sendFileCancellationTokenSource.Token);
                });
                
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

                    Frame.Navigate(typeof(MainSendFailed), JsonConvert.SerializeObject(new SendFailedViewModel
                    {
                        ErrorTitle = title,
                        ErrorDescription = text,
                    }));
                }
            }
            else
            {
                await SendFinishService(packageManager);

                ViewModel.SendStatus = "Done.";
            }

            return result;
        }

        private async Task<bool> DownloadNecessaryFiles()
        {
            try
            {
                retrievingfiles = true;
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
                            await readStream.ReadAsync(buffer, 0, buffer.Length, rertieveCancellationTokenSource.Token);
                        }
                    }
                }

                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            finally
            {
                retrievingfiles = false;
            }            
        }

        private async Task<List<FileSendInfo>> GetFilesWithoutFolderStructure(IEnumerable<IStorageItem> items)
        {
            var output = new List<FileSendInfo>();

            var files = items.Where(x => x is StorageFile)
                .Select(x => new FileSendInfo(new WinRTFile(x as StorageFile)));
            var folders = items.Where(x => x is StorageFolder)
                .Select(x => x as StorageFolder);

            output.AddRange(files);

            foreach (var folder in folders)
            {
                output.AddRange(await GetFilesWithoutFolderStructure(await folder.GetItemsAsync()));
            }

            return output;
        }

        private async Task<List<FileSendInfo>> GetFiles(List<IStorageItem> items)
        {
            var output = new List<FileSendInfo>();

            var files = items.Where(x => x is StorageFile)
                .Select(x => new FileSendInfo(new WinRTFile(x as StorageFile), Path.GetDirectoryName(x.Path)));
            var folders = items.Where(x => x is StorageFolder)
                .Select(x => x as StorageFolder);

            var paths = files
                .Select(x => Path.GetDirectoryName(x.File.Path))
                .Union(folders.Select(x => x.Path))
                .Distinct();
            var rootFolderPath = new string(paths.Select(str => str.TakeWhile((c, index) => paths.All(s => s[index] == c))).FirstOrDefault().ToArray());
            if (folders.Count() == 1 && files.Count() == 0) // In case it's a single folder, exclude the folder name
                rootFolderPath = Path.GetDirectoryName(rootFolderPath) ?? rootFolderPath; 

            output.AddRange(files);
            
            foreach (var folder in folders)
            {
                output.AddRange(await GetFilesOfFolder(folder, rootFolderPath));
            }

            return output;
        }

        private async Task<List<FileSendInfo>> GetFilesOfFolder(StorageFolder f, string rootFolder = null)
        {
            if (rootFolder == null)
                rootFolder = f.Path;

            List<FileSendInfo> files = (from x in await f.GetFilesAsync()
                                        select new FileSendInfo(new WinRTFile(x), rootFolder)).ToList();

            var folders = await f.GetFoldersAsync();

            foreach (var folder in folders)
            {
                files.AddRange(await GetFilesOfFolder(folder, rootFolder));
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
                ViewModel.SendStatus = "Done.";
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
                launchResult = await MainPage.Current.PackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri, rs, false);

            string status;
            if (launchResult == RomeRemoteLaunchUriStatus.Success)
                status = "";
            else
                status = launchResult.ToString();
            SendTextFinished(status);
        }

        private void SendTextFinished(string errorMessage)
        {
            ViewModel.SendStatus = (errorMessage.Length == 0) ? "Done." : errorMessage;
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
