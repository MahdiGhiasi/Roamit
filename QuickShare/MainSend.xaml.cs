using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.FileTransfer;
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
            var result = await MainPage.Current.PackageManager.Connect(rs, true, new Uri("quickshare://wake"));
            
            if (result != RomeAppServiceConnectionStatus.Success)
            {
                await (new MessageDialog("Connection problem : " + result.ToString())).ShowAsync();
                Frame.GoBack();
                return;
            }

            string deviceName = (new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation()).FriendlyName;
            var mode = e.Parameter.ToString();
            ViewModel.UnlockNoticeVisibility = Visibility.Collapsed;

            if (mode == "text")
            {
                TextSender ts = new TextSender(MainPage.Current.PackageManager, deviceName);

                ts.TextSendProgress += (ee) =>
                {
                    ViewModel.ProgressMaximum = ee.TotalParts;
                    ViewModel.ProgressValue = ee.SentParts;
                };

                ViewModel.SendStatus = "Sending...";

                bool sendResult = await ts.Send(SendDataTemporaryStorage.Text, ContentType.ClipboardContent);

                if (sendResult)
                    ViewModel.SendStatus = "Finished.";
                else
                    ViewModel.SendStatus = "Failed :(";

                ViewModel.ProgressValue = ViewModel.ProgressMaximum;
            }
            else if (mode == "launchUri")
            {
                var launchResult = await MainPage.Current.PackageManager.LaunchUri(SendDataTemporaryStorage.LaunchUri);

                if (launchResult == RomeRemoteLaunchUriStatus.Success)
                    ViewModel.SendStatus = "Finished.";
                else
                    ViewModel.SendStatus = launchResult.ToString();
            }
            else if (mode == "file")
            {
                string sendingText = (SendDataTemporaryStorage.Files.Count == 1) ? "Sending file..." : "Sending files...";
                ViewModel.SendStatus = "Preparing...";

                bool failed = false;
                string message = "";

                using (FileSender fs = new FileSender(rs, 
                                                      new QuickShare.UWP.WebServerGenerator(), 
                                                      QuickShare.UWP.Rome.RomePackageManager.Instance, 
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
                    else if (SendDataTemporaryStorage.Files.Count == 1)
                    {
                        await Task.Run(async () =>
                        {
                            if (!await fs.SendFile(new PCLStorage.WinRTFile(SendDataTemporaryStorage.Files[0])))
                                failed = true;
                        });
                    }
                    else
                    {
                        await Task.Run(async () =>
                        {
                            if (!await fs.SendFiles(from x in SendDataTemporaryStorage.Files
                                                    select new PCLStorage.WinRTFile(x), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\"))
                                failed = true;
                        });
                    }

                    ViewModel.ProgressValue = ViewModel.ProgressMaximum;
                }

                Dictionary<string, object> vs = new Dictionary<string, object>();
                vs.Add("Receiver", "System");
                vs.Add("FinishService", "FinishService");
                await MainPage.Current.PackageManager.Send(vs);

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
    }
}
