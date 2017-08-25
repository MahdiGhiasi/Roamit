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
    public sealed partial class SignInNoticeFlyout : UserControl, IFlyout
    {
        public event EventHandler FlyoutCloseRequest;

        public SignInNoticeFlyout()
        {
            this.InitializeComponent();
        }

        private async void Authenticate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                progressRing.Visibility = Visibility.Visible;

                var graph = new Graph(await MSAAuthenticator.GetAccessTokenAsync("User.Read"));
                //await (new MessageDialog(await graph.GetUserUniqueIdAsync())).ShowAsync();
                var userId = await graph.GetUserUniqueIdAsync();
                SecureKeyStorage.SetUserId(userId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authenticate failed. {ex.ToString()}");
                MainPage.Current.IsAskedAboutMSAPermission = false;
                MainPage.Current.isAskedAboutMSAPermissionThisTime = true;
            }
            finally
            {
                this.IsEnabled = true;
                FlyoutCloseRequest?.Invoke(this, new EventArgs());
            }
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
