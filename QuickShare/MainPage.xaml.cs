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
using System.Diagnostics;
using QuickShare.DevicesListManager;
using System.Linq;
using QuickShare.HelperClasses;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using System.Numerics;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.ApplicationModel.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        public RomePackageManager PackageManager { get; } = RomePackageManager.Instance;
        public MainPageViewModel ViewModel { get; set; } = new MainPageViewModel();
        public IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem> PicturePickerItems { get; internal set; }

        public MainPage()
        {
            this.InitializeComponent();

            Current = this;

            Debug.WriteLine("MainPage created.");
        }

        public async Task FileTransferProgress(FileTransferProgressEventArgs e)
        {
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

        internal RemoteSystem GetSelectedSystem()
        {
            return PackageManager.RemoteSystems.FirstOrDefault(x => x.Id == ViewModel.ListManager.SelectedRemoteSystem.Id);
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
            Debug.WriteLine("MainPage loaded begin");

            InitAcrylicUI();

            ContentFrame.Navigate(typeof(MainActions));

            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            DiscoverDevices();
            PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            if (!futureAccessList.ContainsItem("downloadMainFolder"))
            {
                var myfolder = await DownloadsFolder.CreateFolderAsync("QuickShare" + DateTime.Now.Millisecond);
                futureAccessList.AddOrReplace("downloadMainFolder", myfolder);
            }

            PicturePickerItems = new IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem>(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 27 : 80,
                                                                                                          DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 3 : 2);
            await PicturePickerItems.LoadMoreItemsAsync(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? (uint)27 : (uint)80);
        }

        int AcrylicStatus = -1;
        private void InitAcrylicUI()
        {
            DeviceInfo.RefreshFormFactorType();
            if ((DeviceInfo.SystemVersion > DeviceInfo.CreatorsUpdate) && (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop))
            {
                if (AcrylicStatus != 1)
                {
                    AcrylicStatus = 1;
                    ApplyAcrylicAppBar();
                    ViewModel.IsAcrylicEnabled = true;
                }
                ApplyAcrylicAccent();
            }
            else
            {
                if (AcrylicStatus != 0)
                {
                    AcrylicStatus = 0;
                    DisableAcrylicAppBar();
                    ViewModel.IsAcrylicEnabled = false;
                }
            }
        }

        private void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                {
                    ViewModel.ListManager.AddDevice(item);
                }

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                {
                    ViewModel.ListManager.RemoveDevice(item);
                }

            var selItem = PackageManager.RemoteSystems.FirstOrDefault(x => x.Id == ViewModel.ListManager.SelectedRemoteSystem?.Id);
            if ((selItem != null) && (ViewModel.ListManager.SelectedRemoteSystem.IsAvailableByProximity != selItem.IsAvailableByProximity))
                ViewModel.ListManager.Select(selItem);
        }

        private void devicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (devicesList.SelectedItem == null)
                return;

            var s = devicesList.SelectedItem as NormalizedRemoteSystem;

            ViewModel.ListManager.Select(s);

            var sv = VisualChildFinder.FindVisualChild<ScrollViewer>(devicesList);
            sv.ChangeView(0, 0, sv.ZoomFactor, false);
        }

        private async void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if ((e.Content is MainActions) || (e.Content is MainSend))
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
                ViewModel.BackButtonPlaceholderVisibility = Visibility.Collapsed;
            }
            else
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
                ViewModel.BackButtonPlaceholderVisibility = Visibility.Visible;
            }

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

        private async void DiscoverDevices()
        {
            await PackageManager.InitializeDiscovery();
            await Task.Delay(TimeSpan.FromSeconds(1));
            ViewModel.ListManager.SelectHighScoreItem();
            if (ViewModel.ListManager.SelectedRemoteSystem == null)
            {
                //Try again
                await Task.Delay(TimeSpan.FromSeconds(1));
                ViewModel.ListManager.SelectHighScoreItem();
            }
        }

        private void ApplyAcrylicAppBar()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            formattableTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        private void DisableAcrylicAppBar()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = formattableTitleBar.BackgroundColor;
            formattableTitleBar.ButtonInactiveBackgroundColor = formattableTitleBar.InactiveBackgroundColor;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;
        }

        private void ApplyAcrylicAccent()
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _hostSprite = _compositor.CreateSpriteVisual();
            _hostSprite.Size = new Vector2((float)BlurGrid.ActualWidth, (float)BlurGrid.ActualHeight);

            ElementCompositionPreview.SetElementChildVisual(
                    BlurGrid, _hostSprite);
            _hostSprite.Brush = _compositor.CreateHostBackdropBrush();
        }
        Compositor _compositor;
        SpriteVisual _hostSprite;

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitAcrylicUI();
        }
    }
}
