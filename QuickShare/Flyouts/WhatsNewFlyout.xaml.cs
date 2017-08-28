using QuickShare.Common;
using QuickShare.Classes;
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
using QuickShare.HelperClasses;
using Windows.System;
using Windows.ApplicationModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace QuickShare.Flyouts
{
    public sealed partial class WhatsNewFlyout : UserControl, IFlyout
    {
        public event EventHandler FlyoutCloseRequest;

        public WhatsNewFlyout()
        {
            this.InitializeComponent();

            foreach (var item in Content.Children)
            {
                var sp = (item as StackPanel);
                if (sp != null)
                    sp.Visibility = Visibility.Collapsed;
            }

            var pv = DeviceInfo.ApplicationVersion;
            VersionText.Text = $"{pv.Major}.{pv.Minor}";

            if (PCExtensionHelper.IsSupported)
            {
                UniversalClipboardPC.Visibility = Visibility.Visible;
                UniversalClipboardPhone.Visibility = Visibility.Collapsed;
            }
            else
            {
                UniversalClipboardPC.Visibility = Visibility.Collapsed;
                UniversalClipboardPhone.Visibility = Visibility.Visible;
            }
        }

        private void OKButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        public void InitFlyout()
        {
            bool changelogPresent = false;
            var ids = WhatsNewHelper.GetWhatsNewContentId();

            foreach (var item in Content.Children)
            {
                var sp = item as StackPanel;
                if (sp == null)
                    continue;

                if (ids.Contains(sp.Tag.ToString()))
                {
                    changelogPresent = true;
                    sp.Visibility = Visibility.Visible;
                }
                else
                {
                    sp.Visibility = Visibility.Collapsed;
                }
            }

            if (!changelogPresent)
                FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private async void GetPCExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Constants.PCExtensionUrl));
        }

        private async void EnableUniversalClipboard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"] = true;
            await PCExtensionHelper.StartPCExtension();

            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }
    }
}
