using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.RemoteSystems;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using QuickShare.Common;
using QuickShare.UWP.Rome;
using QuickShare.FileTransfer;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        public RomePackageManager packageManager = RomePackageManager.Instance;
        public RemoteSystem selectedSystem = null;

        public IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem> PicturePickerItems { get; internal set; } 

        public MainPage()
        {
            this.InitializeComponent();

            Current = this;
        }

        public async Task FileTransferProgress(FileTransferProgressEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("HandledIt!");
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) //Phone
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                if (e.State == FileTransferState.Finished)
                {
                    statusBar.ProgressIndicator.Text = "";
                    statusBar.ProgressIndicator.ProgressValue = 0;
                    await statusBar.ProgressIndicator.HideAsync();
                }
                else
                {
                    statusBar.ProgressIndicator.Text = "Receiving...";
                    statusBar.ProgressIndicator.ProgressValue = ((double)e.CurrentPart) / (double)(e.Total + 1);
                    await statusBar.ProgressIndicator.ShowAsync();
                }
            }

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) //Desktop
            {
                var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                if (e.State == FileTransferState.Finished)
                {
                    appView.Title = "";
                }
                else
                {
                    appView.Title = "Receiving " + ((int)Math.Round((100.0 * e.CurrentPart) / (e.Total + 1))).ToString() + "%";
                }
            }
        }

        private void MainPage_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if (ContentFrame.Content is MainActions)
                return;

            if (ContentFrame.Content is MainSend)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;
            if (ContentFrame.CanGoBack)
                ContentFrame.GoBack();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(MainActions));

            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            await packageManager.InitializeDiscovery();
            devicesList.ItemsSource = packageManager.RemoteSystems;

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            if (!futureAccessList.ContainsItem("downloadMainFolder"))
            {
                var myfolder = await DownloadsFolder.CreateFolderAsync("QuickShare" + DateTime.Now.Millisecond);
                futureAccessList.AddOrReplace("downloadMainFolder", myfolder);
            }

            PicturePickerItems = new IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem>(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 35 : 75);
            await PicturePickerItems.LoadMoreItemsAsync(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? (uint)35 : (uint)75);
        }

        private void devicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedSystem = devicesList.SelectedItem as RemoteSystem;
            activeDevice.Content = selectedSystem?.DisplayName.ToUpper();
        }

        private void button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
        }

        private async void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if ((e.Content is MainActions) || (e.Content is MainSend))
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
            else
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;

            if (e.Content is MainActions)
            {
                if (BottomBar.Visibility == Visibility.Collapsed) //Don't play animation on app startup
                {
                    BottomBar.Visibility = Visibility.Visible;
                    bottomBarShowStoryboard.Begin();
                }
            }
            else
            {
                bottomBarHideStoryboard.Begin();
                await Task.Delay(400);
                BottomBar.Visibility = Visibility.Collapsed;
            }
        }

    }
}
