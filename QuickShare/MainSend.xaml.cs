using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public MainSend()
        {
            this.InitializeComponent();

            defaultViewModel["SendStatus"] = "Connecting...";
        }

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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

            var rs = MainPage.Current.selectedSystem;
            var result = await MainPage.Current.packageManager.Connect(rs, true);

            if (result != RomeAppServiceConnectionStatus.Success)
            {
                await (new MessageDialog("Connection problem : " + result.ToString())).ShowAsync();
                Frame.GoBack();
                return;
            }

            var mode = e.Parameter.ToString();

            if (mode == "clipboard")
            {

            }
            else if (mode == "file")
            {
                defaultViewModel["SendStatus"] = (MainPage.Current.filesToSend.Count == 1) ? "Sending file..." : "Sending files...";

                bool failed = false;
                string message = "";

                using (FileSender fs = new FileSender(rs, 
                                                      new QuickShare.UWP.WebServerGenerator(), 
                                                      QuickShare.UWP.Rome.RomePackageManager.Instance, 
                                                      FindMyIPAddresses()))
                {
                    fs.FileTransferProgress += (ss, ee) =>
                    {
                        if (ee.State == FileTransferState.Error)
                        {
                            failed = true;
                            message = ee.Message;
                        }
                        else
                        {
                            defaultViewModel["ProgressMaximum"] = ee.Total + 1;
                            defaultViewModel["ProgressValue"] = ee.CurrentPart;
                        }
                    };

                    if (MainPage.Current.filesToSend.Count == 0)
                    {
                        defaultViewModel["SendStatus"] = "No files.";
                        return;
                    }
                    else if (MainPage.Current.filesToSend.Count == 1)
                    {
                        await fs.SendFile(new PCLStorage.WinRTFile(MainPage.Current.filesToSend[0]));
                    }
                    else
                    {
                        await fs.SendFiles(from x in MainPage.Current.filesToSend
                                           select new PCLStorage.WinRTFile(x), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\");
                    }
                    defaultViewModel["ProgressValue"] = Progress.Maximum;
                }

                Dictionary<string, object> vs = new Dictionary<string, object>();
                vs.Add("Receiver", "System");
                vs.Add("FinishService", "FinishService");
                await MainPage.Current.packageManager.Send(vs);

                if (failed)
                {
                    defaultViewModel["SendStatus"] = "Failed.";
                    await (new MessageDialog("Send failed.\r\n\r\n" + message)).ShowAsync();
                }
                else
                {
                    defaultViewModel["SendStatus"] = "Finished.";
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
