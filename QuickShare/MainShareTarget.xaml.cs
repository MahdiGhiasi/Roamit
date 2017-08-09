using GoogleAnalytics;
using QuickShare.ViewModels;
using QuickShare.ViewModels.ShareTarget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainShareTarget : Page, IKindChangeAware
    {
        ShareTargetDetails shareDetails;

        public MainShareTargetViewModel ViewModel { get; set; } = new MainShareTargetViewModel();

        public MainShareTarget()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            shareDetails = e.Parameter as ShareTargetDetails;

            if (shareDetails.Type == StandardDataFormats.StorageItems)
            {
                string s = "???";

                if (SendDataTemporaryStorage.Files != null)
                    s = SendDataTemporaryStorage.Files.Count.ToString() + 
                        " file" + (SendDataTemporaryStorage.Files.Count > 1 ? "s" : "");

                ViewModel.PreviewText = s; 
                ViewModel.ShowShareStorageItem();
            }
            else if ((shareDetails.Type == StandardDataFormats.WebLink) || (shareDetails.Type == StandardDataFormats.ApplicationLink))
            {
                ViewModel.PreviewText = SendDataTemporaryStorage.LaunchUri.OriginalString;
                ViewModel.ShowShareUrl();
            }
            else if (shareDetails.Type == StandardDataFormats.Text)
            {
                ViewModel.PreviewText = SendDataTemporaryStorage.Text;
                ViewModel.ShowShareText();
            }

            base.OnNavigatedTo(e);

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("ShareTarget").Build());
#endif
        }

        private void SendStorageItems_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainSend), "file");
        }

        private void SendText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainSend), "text");
        }

        private void LaunchUrl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainSend), "launchUri");
        }

        private void SendUrl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainSend), "text");
        }

        public void SelectedRemoteSystemChanged(string kind)
        {
            ViewModel.AppActionsAvailable = (kind != "Xbox");
        }
    }
}
