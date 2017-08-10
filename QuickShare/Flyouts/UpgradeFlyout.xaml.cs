using QuickShare.HelperClasses.Version;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace QuickShare.Flyouts
{
    public sealed partial class UpgradeFlyout : UserControl, IFlyout
    {
        public event EventHandler FlyoutCloseRequest;

        public UpgradeFlyout()
        {
            this.InitializeComponent();
        }

        public void InitFlyout(UpgradeFlyoutState state)
        {
            switch (state)
            {
                case UpgradeFlyoutState.WhileSendingFile:
                    FileSizeLimitNotice.Visibility = Visibility.Visible;
                    break;
                case UpgradeFlyoutState.Default:
                default:
                    FileSizeLimitNotice.Visibility = Visibility.Collapsed;
                    break;
            }

            UpgradeButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            progressRing.IsActive = false;
            progressRing.Visibility = Visibility.Collapsed;
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TrialHelper.UpgradeFlyoutCompletion.SetResult(false);
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private async void Upgrade_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UpgradeButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            progressRing.Visibility = Visibility.Visible;
            progressRing.IsActive = true;

            await TrialHelper.TryUpgrade();

            TrialHelper.UpgradeFlyoutCompletion.SetResult(true);
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }
    }
}
