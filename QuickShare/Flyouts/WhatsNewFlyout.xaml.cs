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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace QuickShare.Flyouts
{
    public sealed partial class WhatsNewFlyout : UserControl
    {
        public delegate void FlyoutCloseRequestEventHandler(EventArgs e);
        public event FlyoutCloseRequestEventHandler FlyoutCloseRequest;

        public WhatsNewFlyout()
        {
            this.InitializeComponent();

            foreach (var item in Content.Children)
            {
                var sp = (item as StackPanel);
                if (sp != null)
                    sp.Visibility = Visibility.Collapsed;
            }

            VersionText.Text = DeviceInfo.ApplicationVersionString;
        }

        private void OKButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(new EventArgs());
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
                FlyoutCloseRequest?.Invoke(new EventArgs());
        }

        private async void GetPCExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://roamit.ghiasi.net/#pcExtension"));
        }
    }
}
