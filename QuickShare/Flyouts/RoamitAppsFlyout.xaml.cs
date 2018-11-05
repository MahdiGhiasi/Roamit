using QuickShare.Classes;
using QuickShare.Common;
using QuickShare.HelperClasses;
using QuickShare.MicrosoftGraphFunctions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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
    public sealed partial class RoamitAppsFlyout : UserControl, IFlyout<EventArgs>
    {
        public event EventHandler<EventArgs> FlyoutCloseRequest;

        public RoamitAppsFlyout()
        {
            this.InitializeComponent();
        }

        private void Close_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private async void GetExtension_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await LaunchOperations.LaunchUrl(Constants.BrowserExtensionsUrl);
        }

        //private async void GetForAndroid_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    await LaunchOperations.LaunchUrl(Constants.GooglePlayAppUrl);
        //}

        //private async void GetForWindows_Tapped(object sender, TappedRoutedEventArgs e)
        //{
        //    await LaunchOperations.LaunchUrl(Constants.WindowsStoreAppUrl);
        //}

        private async void GetForWindowsAndAndroid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await LaunchOperations.LaunchUrl(Constants.RoamitHomepageUrl);
        }
    }
}
