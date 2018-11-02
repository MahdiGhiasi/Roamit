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
    public sealed partial class SignInToCloudServiceFlyout : UserControl, IFlyout<SignInToCloudServiceFlyoutResultEventArgs>
    {
        public event EventHandler<SignInToCloudServiceFlyoutResultEventArgs> FlyoutCloseRequest;

        public SignInToCloudServiceFlyout()
        {
            this.InitializeComponent();
        }

        private void SignIn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new SignInToCloudServiceFlyoutResultEventArgs
            {
                ShouldStartSignInProcess = true,
            });
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new SignInToCloudServiceFlyoutResultEventArgs
            {
                ShouldStartSignInProcess = false,
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class SignInToCloudServiceFlyoutResultEventArgs
    {
        public bool ShouldStartSignInProcess { get; set; }
    }
}
