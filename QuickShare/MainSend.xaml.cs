using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.DevicesListManager;
using QuickShare.FileTransfer;
using QuickShare.HelperClasses;
using QuickShare.HelperClasses.VersionHelpers;
using QuickShare.TextTransfer;
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
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainSend : Page
    {
        public MainSendViewModel ViewModel { get; set; }

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
            };
        }

        private void BackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var rs = MainPage.Current.GetSelectedSystem();

            IRomePackageManager packageManager;
            if (rs is NormalizedRemoteSystem)
                packageManager = MainPage.Current.AndroidPackageManager;
            else
                packageManager = MainPage.Current.PackageManager;

            string deviceName = (new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation()).FriendlyName;
            var mode = e.Parameter.ToString();


            if ((mode == "file") && (!(await IsAllowedToSendAsync())))
            {
                ViewModel.SendStatus = "";

                await TrialHelper.AskForUpgradeWhileSending();

                if (!(await IsAllowedToSendAsync()))
                {
                    Frame.GoBack();
                    return;
                }
                ViewModel.SendStatus = "Connecting...";
            }


            if (mode == "launchUri")
            {
                ViewModel.ProgressPercentIndicatorVisibility = Visibility.Collapsed;

                RomeRemoteLaunchUriStatus launchResult;

                if (rs is NormalizedRemoteSystem)
                    launchResult = await UWP.Rome.AndroidRomePackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri, rs as NormalizedRemoteSystem, SecureKeyStorage.GetUserId());
                else
                    launchResult = await MainPage.Current.PackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri, rs);

                if (launchResult == RomeRemoteLaunchUriStatus.Success)
                    ViewModel.SendStatus = "Finished.";
                else
                    ViewModel.SendStatus = launchResult.ToString();

                ViewModel.ProgressIsIndeterminate = false;
                ViewModel.ProgressMaximum = 100;
                ViewModel.ProgressValue = ViewModel.ProgressMaximum;
                ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;
            }
            else
            {
                ViewModel.ProgressPercentIndicatorVisibility = Visibility.Visible;
                RomeAppServiceConnectionStatus result = await Connect(rs);

                if (result != RomeAppServiceConnectionStatus.Success)
                {
                    await (new MessageDialog("Connection problem : " + result.ToString())).ShowAsync();
                    Frame.GoBack();
                    return;
                }

                ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;

                if (mode == "text")
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

                    bool sendResult = await ts.Send(SendDataTemporaryStorage.Text, ContentType.ClipboardContent);

                    if (sendResult)
                        ViewModel.SendStatus = "Finished.";
                    else
                        ViewModel.SendStatus = "Failed :(";

                    ViewModel.ProgressIsIndeterminate = false;
                    ViewModel.ProgressValue = ViewModel.ProgressMaximum;

                    await SendFinishService(packageManager);
                }
                else if (mode == "file")
                {
                    string sendingText = ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFile)) ? "Sending file..." : "Sending files...";
                    ViewModel.SendStatus = "Preparing...";

                    bool failed = false;
                    string message = "";

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
                                failed = true;
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
                            return;
                        }
                        else if ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFile))
                        {
                            await Task.Run(async () =>
                            {
                                if (!await fs.SendFile(new PCLStorage.WinRTFile(SendDataTemporaryStorage.Files[0] as StorageFile)))
                                    failed = true;
                            });
                        }
                        else if ((SendDataTemporaryStorage.Files.Count == 1) && (SendDataTemporaryStorage.Files[0] is StorageFolder))
                        {
                            await Task.Run(async () =>
                            {
                                if (!await fs.SendFolder(new PCLStorage.WinRTFolder(SendDataTemporaryStorage.Files[0] as StorageFolder), ""))
                                    failed = true;
                            });
                        }
                        else
                        {
                            await Task.Run(async () =>
                            {
                                if (!await fs.SendFiles(from x in SendDataTemporaryStorage.Files
                                                        where x is StorageFile
                                                        select new PCLStorage.WinRTFile(x as StorageFile), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\"))
                                    failed = true;
                            });
                        }

                        ViewModel.ProgressValue = ViewModel.ProgressMaximum;
                    }

                    await SendFinishService(packageManager);

                    if (failed)
                    {
                        ViewModel.SendStatus = "Failed.";
                        await (new MessageDialog("Send failed.\r\n\r\n" + message)).ShowAsync();
                    }
                    else
                    {
                        ViewModel.SendStatus = "Finished.";
                    }
                }
                else if (mode == "folder")
                {

                }
                else
                {
                    await (new MessageDialog("MainSend::Invalid parameter.")).ShowAsync();
                    Frame.GoBack();
                }
            }

            if (SendDataTemporaryStorage.IsSharingTarget)
            {
                await Task.Delay(TimeSpan.FromSeconds(1.5));

                App.ShareOperation.ReportCompleted();
            }
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

        private async Task<bool> IsAllowedToSendAsync()
        {
            if (!TrialSettings.IsTrial)
                return true;

            double totalSize = 0;
            foreach (var item in SendDataTemporaryStorage.Files)
            {
                var file = item as StorageFile;
                if (file == null)
                    continue;

                var properties = await file.GetBasicPropertiesAsync();
                totalSize += properties.Size / (1024.0 * 1024.0);

                if (totalSize > Constants.MaxSizeForTrialVersion)
                    return false;
            }

            return true;
        }

        private static async Task<RomeAppServiceConnectionStatus> Connect(object rs)
        {
            if (rs is NormalizedRemoteSystem)
                return await MainPage.Current.AndroidPackageManager.Connect(rs as NormalizedRemoteSystem,
                    SecureKeyStorage.GetUserId(),
                    MainPage.Current.PackageManager.RemoteSystems.Where(x => x.Kind != "Unknown").Select(x => x.Id));
            else
                return await MainPage.Current.PackageManager.Connect(rs, true, new Uri("quickshare://wake"));
        }
    }
}
