using QuickShare.Common;
using QuickShare.HelperClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class CloudServiceLogin : Page
    {
        bool sendCloudClipboardOriginalValue;

        public CloudServiceLogin()
        {
            this.InitializeComponent();

            navigationFailedGrid.Visibility = Visibility.Collapsed;
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            loadingProgress.IsIndeterminate = true;
            loadingProgress.Visibility = Visibility.Visible;
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if ((args.Uri.AbsolutePath.Contains("/v2/Graph/Welcome")) && (args.Uri.Query.Length > 12))
            {
                webView.Visibility = Visibility.Collapsed;

                string id = args.Uri.Query.Substring(11);
                Debug.WriteLine($"Account Id is: {id}");

                SecureKeyStorage.SetAccountId(id);

                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SendCloudClipboard") &&
                    ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"].ToString().ToLower() == "true")
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    PCExtensionHelper.StartPCExtension();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                    if (PCExtensionHelper.IsSupported)
                        ToastFunctions.SendUniversalClipboardNoticeToast();
                }
                Frame.GoBack();
            }
            else
            {
                loadingProgress.IsIndeterminate = false;
                loadingProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            loadingProgress.IsIndeterminate = false;
            loadingProgress.Visibility = Visibility.Collapsed;

            navigationFailedGrid.Visibility = Visibility.Visible;
            navigationFailedMessage.Text = e.WebErrorStatus.ToString();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAuthenticateGraphPage();
        }

        private void LoadAuthenticateGraphPage()
        {
            webView.Navigate(new Uri($"{Constants.ServerAddress}/v2/Authenticate/Graph"));
        }

        private void Retry_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LoadAuthenticateGraphPage();
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
