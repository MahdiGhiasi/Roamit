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
using QuickShare.Classes;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using System.Numerics;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.ApplicationModel.Core;
using QuickShare.MicrosoftGraphFunctions;
using Windows.UI.Popups;
using Microsoft.Graphics.Canvas.Effects;
using Windows.ApplicationModel.DataTransfer;
using GoogleAnalytics;
using Windows.UI.Core;
using QuickShare.ViewModels;
using QuickShare.HelperClasses.Version;
using QuickShare.ViewModels.ShareTarget;
using QuickShare.HelperClasses;
using QuickShare.Classes.ItemSources;
using QuickShare.ViewModels.PicturePicker;

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
        public AndroidRomePackageManager AndroidPackageManager { get; } = AndroidRomePackageManager.Instance;
        public MainPageViewModel ViewModel { get; set; } = new MainPageViewModel();
        public IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem> PicturePickerItems { get; internal set; }

        public object InternalFrameContent { get => ContentFrame.Content; }

        public bool IsShareContent { get; private set; } = false;
        bool loadWait = false;

        public static bool IsUserSelectedRemoteSystemManually { get; set; } = false;
        int remoteSystemPrevCount = 0;

        bool discoverOtherDevicesResult = true;

        public bool isAskedAboutMSAPermissionThisTime = false;

        public bool IsAskedAboutMSAPermission
        {
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("IsAskedAboutMSAPermission"))
                    ApplicationData.Current.LocalSettings.Values["IsAskedAboutMSAPermission"] = false;

                return (bool)ApplicationData.Current.LocalSettings.Values["IsAskedAboutMSAPermission"];
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["IsAskedAboutMSAPermission"] = value;
                isAskedAboutMSAPermissionThisTime = true;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.Closed += Window_Closed;
        }

        public async Task FileTransferProgress(FileTransfer2ProgressEventArgs e)
        {
            if (ContentFrame.CurrentSourcePageType == typeof(MainReceive))
            {
                (ContentFrame.Content as MainReceive).FileTransferProgress(e);
                return;
            }

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) //Phone
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                if (e.State == FileTransferState.Finished)
                {
                    PersistentDisplay.ReleasePersistentDisplay();

                    statusBar.ProgressIndicator.Text = "";
                    statusBar.ProgressIndicator.ProgressValue = 0;
                    await statusBar.ProgressIndicator.HideAsync();
                }
                else if (e.State == FileTransferState.Error)
                {
                    PersistentDisplay.ReleasePersistentDisplay();

                    statusBar.ProgressIndicator.Text = "";
                    statusBar.ProgressIndicator.ProgressValue = 0;
                    await statusBar.ProgressIndicator.HideAsync();
                }
                else
                {
                    PersistentDisplay.ActivatePersistentDisplay();

                    statusBar.ProgressIndicator.Text = "Receiving...";
                    statusBar.ProgressIndicator.ProgressValue = e.Progress;
                    await statusBar.ProgressIndicator.ShowAsync();
                }
            }

            //if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) //Desktop
            //{
            //    var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            //    if (e.State == FileTransferState.Finished)
            //    {
            //        appView.Title = "";
            //    }
            //    else
            //    {
            //        appView.Title = "Receiving " + ((int)Math.Round((100.0 * e.CurrentPart) / (e.Total + 1))).ToString() + "%";
            //    }
            //}
            if (e.State == FileTransferState.Finished)
            {
                ViewModel.Caption = "";
            }
            else
            {
                ViewModel.Caption = $"Receiving {(int)Math.Round(100.0 * e.Progress)}%";
            }
        }

        internal object GetSelectedSystem()
        {
            var rs = PackageManager.RemoteSystems.FirstOrDefault(x => x.Id == ViewModel.ListManager.SelectedRemoteSystem.Id);
            if (rs == null)
                return ViewModel.ListManager.SelectedRemoteSystem;
            return rs;
        }

        private void MainPage_BackRequested(object sender, Windows.UI.Core.BackRequestedEventArgs e)
        {
            if ((ContentFrame.Content is MainActions) || (ContentFrame.Content is MainShareTarget))
                return;

            if (ContentFrame.Content is MainSendFailed)
                while (ContentFrame.BackStackDepth > 0 && ContentFrame.BackStack[ContentFrame.BackStackDepth - 1].SourcePageType == typeof(MainSend))
                    ContentFrame.BackStack.RemoveAt(ContentFrame.BackStackDepth - 1);

            e.Handled = true;
            if (ContentFrame.CanGoBack)
                ContentFrame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            loadWait = false;
            InitAcrylicUI();
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            if (e.Parameter is ShareTargetDetails)
            {
                IsShareContent = true;

                ContentFrame.Navigate(typeof(MainShareTarget), e.Parameter);
                DispatcherEx.CustomDispatcher = Dispatcher;

                BottomCommandBar.Visibility = Visibility.Collapsed;

                if ((Current != null) && (Current != this) && (Current.IsShareContent == false)) //Main window is present
                {
                    loadWait = true;
                    await Current.WaitForShare();
                    await Task.Delay(2000);
                }

                Current = this;
            }
            else if ((e.Parameter != null) && (e.Parameter.ToString() == "BackFromShareTarget"))
            {
                loadWait = true;
                ContentFrame.Navigate(typeof(MainActions));
                await Task.Delay(2000);

                Current = this;
            }
            else if ((e.Parameter != null) && (e.Parameter.ToString() == "settings"))
            {
                ContentFrame.Navigate(typeof(Settings));
                ContentFrame.BackStack.Clear();
                ContentFrame.BackStack.Add(new PageStackEntry(typeof(MainActions), "", null));

                Current = this;
            }
            else if ((e.Parameter != null) && (e.Parameter.ToString() == "history"))
            {
                ContentFrame.Navigate(typeof(HistoryPage));
                ContentFrame.BackStack.Clear();
                ContentFrame.BackStack.Add(new PageStackEntry(typeof(MainActions), "", null));

                Current = this;
            }
            else if ((e.Parameter != null) && (e.Parameter.ToString() == "receiveDialog"))
            {
                ContentFrame.Navigate(typeof(MainReceive), Frame.BackStackDepth > 0);
                ContentFrame.BackStack.Clear();

                Current = this;
            }
            else if ((ApplicationData.Current.LocalSettings.Values.ContainsKey("SendCloudClipboard")) && (bool.TryParse(ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"].ToString(), out bool scc)) && (scc == true) && (!SecureKeyStorage.IsAccountIdStored()))
            {
                ContentFrame.Navigate(typeof(CloudServiceLogin));
                ContentFrame.BackStack.Clear();
                ContentFrame.BackStack.Add(new PageStackEntry(typeof(MainActions), "", null));

                Current = this;
            }
            else
            {            
                ContentFrame.Navigate(typeof(MainActions));
                Current = this;

                await PCExtensionHelper.StartPCExtension();
            }

            base.OnNavigatedTo(e);

            StorageFolder clipboardTempFolder = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("ClipboardTemp")) as StorageFolder;
            if (clipboardTempFolder != null)
                await clipboardTempFolder.DeleteAsync();
        }

        private void ShowWhatsNewFlyout()
        {
            WhatsNewFlyoutInstance.InitFlyout();
            ViewModel.WhatsNewVisibility = Visibility.Visible;
            overlayShowStoryboard.Begin();
        }

        public async Task WaitForShare()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Frame.Navigate(typeof(ShareWaiting));
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loadWait)
                await Task.Delay(2000);

            if ((!IsShareContent) && (WhatsNewHelper.ShouldShowWhatsNew()))
            {
                ShowWhatsNewFlyout();
            }

            DiscoverDevices();
            InitDiscoveringOtherDevices();
            PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("MainPage").Build());
            if (App.LaunchTime != null)
            {
                var loadTime = DateTime.Now - (DateTime)App.LaunchTime;
                App.LaunchTime = null;
                string loadTimeString = $"{(int)Math.Floor(loadTime.TotalSeconds)}.{(int)Math.Floor(loadTime.Milliseconds / 100.0)}";
                App.Tracker.Send(HitBuilder.CreateCustomEvent("AppLoadTime", loadTimeString).Build());
            }
#endif
            
            PicturePickerItems = new IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem>(item => item.IsAvailable,
                DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 27 : 80,
                DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 3 : 2);
            await PicturePickerItems.LoadMoreItemsAsync(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? (uint)27 : (uint)80);
        }

        int AcrylicStatus = -1;
        private void InitAcrylicUI()
        {
            try
            {
                DeviceInfo.RefreshFormFactorType();
                if ((DeviceInfo.SystemVersion > DeviceInfo.CreatorsUpdate) && (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop))
                {
                    if (AcrylicStatus != 1)
                    {
                        AcrylicStatus = 1;
                        WindowTopBarFunctions.ApplyAcrylic();
                        ViewModel.IsAcrylicEnabled = true;
                    }
                    ApplyAcrylicAccent();
                }
                else
                {
                    if (AcrylicStatus != 0)
                    {
                        AcrylicStatus = 0;
                        WindowTopBarFunctions.DisableAcrylic();
                        ViewModel.IsAcrylicEnabled = false;
                    }
                }
            }
            catch { }
        }

        private async void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
            {
                try
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

                    if ((ViewModel.ListManager.RemoteSystems.Count > 0) && (!IsUserSelectedRemoteSystemManually) && (ViewModel.ListManager.RemoteSystems.Count > remoteSystemPrevCount) && (AllowedToChangeSelectedRemoteSystem()))
                    {
                        remoteSystemPrevCount = ViewModel.ListManager.RemoteSystems.Count;
                        ViewModel.ListManager.SelectHighScoreItem();
                    }

                    ViewModel.RemoteSystemCollectionChanged();

                    CheckIfShouldAskAboutMSAPermission();
                }
                catch { }
            });
        }

        internal void BeTheShareTarget(ShareTargetDetails shareTargetDetails)
        {
            IsShareContent = true;
            ContentFrame.Navigate(typeof(MainShareTarget), shareTargetDetails);
        }

        private void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (devicesList.SelectedItem == null)
                return;

            IsUserSelectedRemoteSystemManually = true;

            var s = devicesList.SelectedItem as NormalizedRemoteSystem;

            ViewModel.ListManager.Select(s);

            var sv = VisualChildFinder.FindVisualChild<ScrollViewer>(devicesList);
            sv.ChangeView(0, 0, sv.ZoomFactor, false);

            ViewModel.RemoteSystemCollectionChanged();
        }

        private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (ContentFrame.Content is CloudServiceLogin)
            {
                if ((ViewModel.ListManager.RemoteSystems.Count > 0) && (!IsUserSelectedRemoteSystemManually))
                    ViewModel.ListManager.SelectHighScoreItem();
            }

            ViewModel.FrameBottomPaddingEnabled = e.SourcePageType == typeof(MainActions);
        }

        private async void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if ((e.Content is MainActions) || (e.Content is MainShareTarget) || (e.Content is MainReceive))
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed;
                ViewModel.BackButtonPlaceholderVisibility = Visibility.Collapsed;
            }
            else
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible;
                ViewModel.BackButtonPlaceholderVisibility = Visibility.Visible;
            }

            if ((e.Content is MainActions) || (e.Content is MainShareTarget))
            {
                if ((BottomBar.Visibility == Visibility.Collapsed) || (Frame.BackStackDepth != 0)) //Don't play animation on app startup, except after Intro.
                {
                    BottomCommandBar.Visibility = Visibility.Visible;
                    BottomBar.Visibility = Visibility.Visible;
                    bottomBarShowStoryboard.Begin();
                }
            }
            else
            {
                bottomBarHideStoryboard.Begin();
                await Task.Delay(400);
                BottomBar.Visibility = Visibility.Collapsed;
                BottomCommandBar.Visibility = Visibility.Collapsed;
            }

            ViewModel.ContentFrameNeedsRemoteSystemSelection = !(((e.Content is Settings) || (e.Content is HistoryPage) || (e.Content is DevicesSettings) || (e.Content is CloudServiceLogin)));
            ViewModel.RemoteSystemCollectionChanged();
        }

        private async void DiscoverDevices()
        {
            var result = await PackageManager.InitializeDiscovery();
            if (result == false)
            {
                await (new MessageDialog("Please make sure your account is logged in into your Microsoft account, and 'Shared Experiences' is enabled in system settings, then restart the app.", "Cannot discover devices.")).ShowAsync();
            }
        }

        bool alreadyDiscovered = false;
        internal async Task<bool> DiscoverOtherDevices(bool force = false, bool forceSelectHighScore = false)
        {
            if (!SecureKeyStorage.IsUserIdStored())
                return false;

            if (alreadyDiscovered && !force)
                return true;
            alreadyDiscovered = true;

            var userId = SecureKeyStorage.GetUserId();
            var devices = await Common.Service.Device.GetAndroidDevices(userId);

            foreach (var item in devices)
                if (ViewModel.ListManager.RemoteSystems.FirstOrDefault(x => x.Id == item.Id) == null) //if not already exists
                    ViewModel.ListManager.AddDevice(item);

            if ((ViewModel.ListManager.RemoteSystems.Count > 0) && (((!IsUserSelectedRemoteSystemManually) && (AllowedToChangeSelectedRemoteSystem())) || forceSelectHighScore))
                ViewModel.ListManager.SelectHighScoreItem();
            ViewModel.RemoteSystemCollectionChanged();

            PackageManagerHelper.InitAndroidPackageManagerMode();
            if (AndroidPackageManager.Mode == AndroidRomePackageManager.AndroidPackageManagerMode.MessageCarrier)
                await Common.Service.Device.WakeAndroidDevices(userId);

            return true;
        }

        private bool AllowedToChangeSelectedRemoteSystem()
        {
            return (ContentFrame.Content is MainActions) || (ContentFrame.Content is Settings) || (ContentFrame.Content is MainShareTarget);
        }

        private async void InitDiscoveringOtherDevices()
        {
            discoverOtherDevicesResult = await DiscoverOtherDevices();
            CheckIfShouldAskAboutMSAPermission();
        }

        private void CheckIfShouldAskAboutMSAPermission()
        {
            if ((ViewModel.ListManager.IsAndroidDevicePresent) && (!IsAskedAboutMSAPermission) && (!isAskedAboutMSAPermissionThisTime) && (!discoverOtherDevicesResult))
            {
                IsAskedAboutMSAPermission = true;
                ShowSignInFlyout();
            }
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

        private void ShowSignInFlyout()
        {
            ViewModel.SignInNoticeVisibility = Visibility.Visible;
            overlayShowStoryboard.Begin();
        }

        private async void SignInNoticeFlyout_FlyoutCloseRequest(object sender, EventArgs e)
        {
            DiscoverOtherDevices();

            overlayHideStoryboard.Begin();
            await Task.Delay(250);
            ViewModel.SignInNoticeVisibility = Visibility.Collapsed;
        }

        private void MainGrid_DragOver(object sender, DragEventArgs e)
        {
            if (ViewModel.ListManager.SelectedRemoteSystem == null)
                return;

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = $"Drop here to send to {ViewModel.ListManager.SelectedRemoteSystem.DisplayName}";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
        }

        private async void MainGrid_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.ListManager.SelectedRemoteSystem == null)
                return;

            string type = await ExternalContentHelper.SetData(e.DataView);

            if (type == StandardDataFormats.StorageItems)
            {
                ContentFrame.Navigate(typeof(MainSend), "file");
            }
            else if ((type == StandardDataFormats.WebLink) || (type == StandardDataFormats.ApplicationLink))
            {
                ContentFrame.Navigate(typeof(MainSend), "launchUri");
            }
            else if (type == StandardDataFormats.Text)
            {
                ContentFrame.Navigate(typeof(MainSend), "text");
            }
        }

        private void SettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(Settings));
        }

        private void Window_Closed(object sender, CoreWindowEventArgs e)
        {
            if (Current == this)
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested -= MainPage_BackRequested;
                PackageManager.RemoteSystems.CollectionChanged -= RemoteSystems_CollectionChanged;

                ViewModel.Dispose();

                Current = null;
            }
        }

        private void HistoryButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(HistoryPage));
        }

        private async void WhatsNewFlyout_FlyoutCloseRequest(object sender, EventArgs e)
        {
            if (ViewModel.SignInNoticeVisibility == Visibility.Visible)
            {
                ViewModel.WhatsNewVisibility = Visibility.Collapsed;
            }
            else
            {
                overlayHideStoryboard.Begin();
                await Task.Delay(250);
                ViewModel.WhatsNewVisibility = Visibility.Collapsed;
            }
        }

        private async void LookingForDevicesHelp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dlg = new MessageDialog("Please check your internet connection.\r\n\r\nIf your internet connection is working, then check the following: \r\n" +
                $"{Convert.ToChar(8226)} On your Windows devices, make sure your user account is connected to your Microsoft account, 'Continue App Experiences' is activated in system settings, and the operating system is updated to latest version.\r\n" +
                $"{Convert.ToChar(8226)} On your Android devices, make sure you opened Roamit and logged in with your Microsoft account.", "Not seeing your devices?");
            await dlg.ShowAsync();
        }

        private async void DonateFlyoutInstance_FlyoutCloseRequest(object sender, EventArgs e)
        {
            overlayHideStoryboard.Begin();
            await Task.Delay(250);
            ViewModel.DonateFlyoutVisibility = Visibility.Collapsed;
        }

        private void ShowDonateFlyout()
        {
            DonateFlyoutInstance.InitFlyout();
            ViewModel.DonateFlyoutVisibility = Visibility.Visible;
            overlayShowStoryboard.Begin();
        }

        private void DonateButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ShowDonateFlyout();
        }

        private void ListExpandCollapseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ViewModel.IsDevicesListExpanded)
                devicesListCollapseStoryboard.Begin();
            else
                devicesListExpandStoryboard.Begin();

            ViewModel.IsDevicesListExpanded = !ViewModel.IsDevicesListExpanded;
        }
    }
}
