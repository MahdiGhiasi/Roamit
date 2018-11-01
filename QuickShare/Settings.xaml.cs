using GoogleAnalytics;
using QuickShare.Classes;
using QuickShare.Common;
using QuickShare.HelperClasses;
using QuickShare.HelperClasses.Version;
using QuickShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
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
    public sealed partial class Settings : Page
    {
        public SettingsViewModel Model { get; set; } = new SettingsViewModel();

        public Settings()
        {
            this.InitializeComponent();
        }

        private async void PrivacyPolicyButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("PrivacyPolicy").Build());
#endif
            await Launcher.LaunchUriAsync(new Uri("https://roamit.ghiasi.net/privacy/"));
        }

        private async void SendFeedbackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
            {
                var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
                await launcher.LaunchAsync();
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "LaunchedFeedbackHub").Build());
#endif
            }
            else
            {
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "FailedToLaunchFeedbackHub").Build());
#endif
                var dlg = new MessageDialog("Try sending feedback from a Windows 10 PC or phone.", "Feedback Hub is not supported on this device");
                await dlg.ShowAsync();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            VisualChildFinder.FindVisualChild<ContentControl>(pivot, "HeaderClipper").MaxWidth = 720;

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("Settings").Build());
#endif
        }

        private async void RateAndReviewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "RateAndReview").Build());
#endif
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
        }

        private async void ContactButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"mailto:roamitapp@gmail.com?subject={Model.PackageName}%20v{Model.PackageVersion}"));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "Contact").Build());
#endif
        }

        private async void GetChromeFirefoxExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://roamit.ghiasi.net/#browserExtensions"));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "GetBrowserExtensions").Build());
#endif
        }

        private async void GetPCExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Common.Constants.PCExtensionUrl));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "GetPCExtension").Build());
#endif
        }

        private async void TwitterButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Common.Constants.TwitterUrl));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "Twitter").Build());
#endif
        }

        private async void ChooseDownloadFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FolderPicker fp = new FolderPicker()
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            fp.FileTypeFilter.Add("*");
            var selectedFolder = await fp.PickSingleFolderAsync();

            if (selectedFolder == null)
                return;

            var downloadFolder = await DownloadFolderHelper.TrySetDefaultDownloadFolderAsync(selectedFolder);
            Model.DefaultDownloadLocation = downloadFolder.Path;
        }

        private void ManageDevices_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DevicesSettings));
        }

        private void SignInToCloudService_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(CloudServiceLogin));
        }

        private async void SignOutFromCloudService_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await PCExtensionHelper.StopPCExtensionIfRunning();

            SecureKeyStorage.DeleteAccountId();
            SecureKeyStorage.DeleteToken();

            ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"] = false.ToString();
            Model.SendCloudClipboard = false;
            Model.RefreshCloudClipboardBindings();
        }

        private async void GitHubButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Common.Constants.GitHubUrl));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "GitHub").Build());
#endif
        }

        private async void GitHubIssueButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Common.Constants.GitHubIssuesUrl));

#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "Link", "GitHubIssues").Build());
#endif
        }
    }
}
