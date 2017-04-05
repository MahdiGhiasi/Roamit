using QuickShare.FileSendReceive;
using QuickShare.Rome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using QuickShare.Common;

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

        public List<StorageFile> filesToSend = new List<StorageFile>();

        public IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem> PicturePickerItems { get; internal set; } 

        public MainPage()
        {
            this.InitializeComponent();

            Current = this;
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
                var myfolder = await DownloadsFolder.CreateFolderAsync("QuickShare");
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
