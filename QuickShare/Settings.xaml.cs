using GoogleAnalytics;
using QuickShare.HelperClasses;
using QuickShare.HelperClasses.VersionHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public SettingsModel Model { get; set; } = new SettingsModel();

        public Settings()
        {
            this.InitializeComponent();
        }

        private async void UpgradeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "TryUpgrade", "Windows").Build());
#endif
            await TrialHelper.AskForUpgrade();

            Model.CheckTrialStatus();
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
        }

        private async void GetChromeFirefoxExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://roamit.ghiasi.net/#browserExtensions"));
        }
    }
}
