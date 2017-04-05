using QuickShare.Common;
using QuickShare.FileSendReceive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var rs = MainPage.Current.selectedSystem;
            var result = await MainPage.Current.packageManager.Connect(rs, true);

            if (result != Windows.ApplicationModel.AppService.AppServiceConnectionStatus.Success)
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

                using (FileSender fs = new FileSender(rs))
                {

                    fs.FileTransferProgress += (ss, ee) =>
                    {
                        defaultViewModel["ProgressMaximum"] = ee.Total + 1;
                        defaultViewModel["ProgressValue"] = ee.CurrentPart;
                    };

                    if (MainPage.Current.filesToSend.Count == 0)
                    {
                        defaultViewModel["SendStatus"] = "No files.";
                        return;
                    }
                    else if (MainPage.Current.filesToSend.Count == 1)
                    {
                        await fs.SendFile(MainPage.Current.filesToSend[0]);
                    }
                    else
                    {
                        await fs.SendFiles(MainPage.Current.filesToSend, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\");
                    }
                    defaultViewModel["ProgressValue"] = Progress.Maximum;
                }

                ValueSet vs = new ValueSet();
                vs.Add("Receiver", "System");
                vs.Add("FinishService", "FinishService");
                await MainPage.Current.packageManager.Send(vs);

                defaultViewModel["SendStatus"] = "Finished.";
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
