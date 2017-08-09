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

        bool isUserSelectedRemoteSystemManually = false;
        int remoteSystemPrevCount = 0;

        bool discoverOtherDevicesResult = true;

        public bool isAskedAboutMSAPermissionThisTime = false;

        AdDuplex.AdControl adDuplexControl = null;

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

#if DEBUG
            AdBanner.ApplicationId = "3f83fe91-d6be-434d-a0ae-7351c5a997f1";
            AdBanner.AdUnitId = "test";

#else
            AdBanner.ApplicationId = AdConstants.MicrosoftAdsAppId;
            AdBanner.AdUnitId = AdConstants.MicrosoftAdsUnitId;

#endif

            Window.Current.Closed += Window_Closed;
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
                ViewModel.Caption = "Receiving " + ((int)Math.Round((100.0 * e.CurrentPart) / (e.Total + 1))).ToString() + "%";
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

            e.Handled = true;
            if (ContentFrame.CanGoBack)
                ContentFrame.GoBack();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            loadWait = false;
            InitAcrylicUI();
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;

            if (!TrialSettings.IsTrial)
                AdFrame.Visibility = Visibility.Collapsed;

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
            }
            else if ((e.Parameter != null) && (e.Parameter.ToString() == "BackFromShareTarget"))
            {
                loadWait = true;
                ContentFrame.Navigate(typeof(MainActions));
                await Task.Delay(2000);
            }
            else
            {
                ContentFrame.Navigate(typeof(MainActions));
            }
            Current = this;

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

            if (!TrialSettings.IsTrial)
                AdBanner.Suspend();

<<<<<<< HEAD
            if ((!IsShareContent) && (WhatsNewHelper.ShouldShowWhatsNew()))
            {
                ShowWhatsNewFlyout();
            }

=======
>>>>>>> UWP: Measure app launch time and send it to Google Analytics
            DiscoverDevices();
            InitDiscoveringOtherDevices();
            PackageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

            await DownloadFolderHelper.InitDownloadFolderAsync();

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("MainPage").Build());
<<<<<<< HEAD
            if (App.LaunchTime != null)
            {
                var loadTime = DateTime.Now - (DateTime)App.LaunchTime;
                App.LaunchTime = null;
                string loadTimeString = $"{(int)Math.Floor(loadTime.TotalSeconds)}.{(int)Math.Floor(loadTime.Milliseconds / 100.0)}";
=======

            if (App.LaunchTime != null)
            {
                App.LaunchTime = null;
                var loadTime = DateTime.Now - (DateTime)App.LaunchTime;
                string loadTimeString = $"{loadTime.TotalSeconds}.{loadTime.Milliseconds / 100}";
>>>>>>> UWP: Measure app launch time and send it to Google Analytics
                App.Tracker.Send(HitBuilder.CreateCustomEvent("AppLoadTime", loadTimeString).Build());
            }
            AdDuplex.AdDuplexClient.Initialize(AdConstants.AdDuplexAppKey);
#endif

            PicturePickerItems = new IncrementalLoadingCollection<PicturePickerSource, PicturePickerItem>(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 27 : 80,
                                                                                                          DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 3 : 2);
            await PicturePickerItems.LoadMoreItemsAsync(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? (uint)27 : (uint)80);

            TrialSettings.IsTrialChanged += TrialSettings_IsTrialChanged;
            TrialHelper.CheckIfFullVersion();
        }

        private async void TrialSettings_IsTrialChanged()
        {
            try
            {
                if (TrialSettings.IsTrial)
                    ShowAds();
                else
                    HideAds();

                if (SecureKeyStorage.IsUserIdStored())
                    await Common.Service.UpgradeDetails.SetUpgradeStatus(SecureKeyStorage.GetUserId(), !TrialSettings.IsTrial);
            }
            catch { } //Temporary fix for share window threading issues.
        }

        private void HideAds()
        {
            AdBanner.Suspend();
            ViewModel.UpgradeButtonVisibility = Visibility.Collapsed;
            AdFrame.Visibility = Visibility.Collapsed;
        }

        private void ShowAds()
        {
            AdBanner.Resume();
            ViewModel.UpgradeButtonVisibility = Visibility.Visible;
            AdFrame.Visibility = Visibility.Visible;
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

                    if ((ViewModel.ListManager.RemoteSystems.Count > 0) && (!isUserSelectedRemoteSystemManually) && (ViewModel.ListManager.RemoteSystems.Count > remoteSystemPrevCount) && (AllowedToChangeSelectedRemoteSystem()))
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

            isUserSelectedRemoteSystemManually = true;

            var s = devicesList.SelectedItem as NormalizedRemoteSystem;

            ViewModel.ListManager.Select(s);

            var sv = VisualChildFinder.FindVisualChild<ScrollViewer>(devicesList);
            sv.ChangeView(0, 0, sv.ZoomFactor, false);

            ViewModel.RemoteSystemCollectionChanged();
        }

        private async void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if ((e.Content is MainActions) || (e.Content is MainShareTarget))
            {
                ViewModel.RefreshIsContentFrameEnabled();

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

            ViewModel.ContentFrameNeedsRemoteSystemSelection = !(((e.Content is Settings) || (e.Content is HistoryPage)));
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
        internal async Task<bool> DiscoverOtherDevices(bool force = false)
        {
            if (!SecureKeyStorage.IsUserIdStored())
                return false;

            if (alreadyDiscovered && !force)
                return true;
            alreadyDiscovered = true;

            var userId = SecureKeyStorage.GetUserId();
            var devices = await Common.Service.DevicesLoader.GetAndroidDevices(userId);

            foreach (var item in devices)
                if (ViewModel.ListManager.RemoteSystems.FirstOrDefault(x => x.Id == item.Id) == null) //if not already exists
                    ViewModel.ListManager.AddDevice(item);

            if ((ViewModel.ListManager.RemoteSystems.Count > 0) && (!isUserSelectedRemoteSystemManually) && (AllowedToChangeSelectedRemoteSystem()))
                ViewModel.ListManager.SelectHighScoreItem();
            ViewModel.RemoteSystemCollectionChanged();

            await Common.Service.DevicesLoader.WakeAndroidDevices(userId);

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

        private async void SignInNoticeFlyout_FlyoutCloseRequest(EventArgs e)
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

        private async void UpgradeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await TrialHelper.AskForUpgrade();
        }

        private void Window_Closed(object sender, CoreWindowEventArgs e)
        {
            if (Current == this)
            {
                Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested -= MainPage_BackRequested;
                PackageManager.RemoteSystems.CollectionChanged -= RemoteSystems_CollectionChanged;
                TrialSettings.IsTrialChanged -= TrialSettings_IsTrialChanged;

                ViewModel.Dispose();

                Current = null;
            }
        }

        private void HistoryButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(HistoryPage));
        }

        private async void AdBanner_ErrorOccurred(object sender, Microsoft.Advertising.WinRT.UI.AdErrorEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowAdDuplexBanner();
            });

            Debug.WriteLine($"AdBanner load error '{e.ErrorCode}': '{e.ErrorMessage}'");
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("MicrosoftAd", "Error", e.ErrorCode.ToString()).Build());
#endif
        }

        private void ShowAdDuplexBanner()
        {
            AdBanner.Visibility = Visibility.Collapsed;
            AdBanner.Suspend();

            AdDuplexContainer.Visibility = Visibility.Visible;

            adDuplexControl = new AdDuplex.AdControl()
            {
                Height = 300,
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                AppKey = AdConstants.AdDuplexAppKey,
                AdUnitId = AdConstants.AdDuplexUnitId,
#if DEBUG
                IsTest = true,
#else
                    IsTest = false,
#endif
            };
            adDuplexControl.AdLoadingError += AdDuplexBanner_AdLoadingError;
            adDuplexControl.NoAd += AdDuplexBanner_NoAd;
            adDuplexControl.AdLoaded += AdDuplexBanner_AdLoaded;
            adDuplexControl.AdCovered += AdDuplexBanner_AdCovered;

            AdDuplexContainer.Children.Add(adDuplexControl);
        }

        private void AdDuplexBanner_AdLoadingError(object sender, AdDuplex.Common.Models.AdLoadingErrorEventArgs e)
        {
            Debug.WriteLine($"AdDuplexBanner load error: '{e.Error}'");
        }

        private void AdDuplexBanner_NoAd(object sender, AdDuplex.Common.Models.NoAdEventArgs e)
        {
            Debug.WriteLine($"AdDuplexBanner NoAd: {e.Message}");
        }

        private void AdDuplexBanner_AdLoaded(object sender, AdDuplex.Banners.Models.BannerAdLoadedEventArgs e)
        {
            Debug.WriteLine($"AdDuplexBanner Ad loaded: {e.NewAd.Url}");
            AdBannerContainer.Visibility = Visibility.Collapsed;
        }

        private void AdDuplexBanner_AdCovered(object sender, AdDuplex.Banners.Core.AdCoveredEventArgs e)
        {
            Debug.WriteLine($"AdDuplexBanner Ad covered: {e.CulpritElement.Name ?? "null"}");
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (adDuplexControl != null)
            {
                adDuplexControl.AdLoadingError -= AdDuplexBanner_AdLoadingError;
                adDuplexControl.NoAd -= AdDuplexBanner_NoAd;
                adDuplexControl.AdLoaded -= AdDuplexBanner_AdLoaded;
                adDuplexControl.AdCovered -= AdDuplexBanner_AdCovered;
            }

            base.OnNavigatingFrom(e);
        }

        private async void WhatsNewFlyout_FlyoutCloseRequest(EventArgs e)
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
    }
}
