using QuickShare.Desktop.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QuickShare.Desktop
{
    /// <summary>
    /// Interaction logic for SignInWindow.xaml
    /// </summary>
    public partial class SignInWindow : Window
    {
        public SignInWindow()
        {
            InitializeComponent();

            Page1.Visibility = Visibility.Visible;
            Page2.Visibility = Visibility.Collapsed;
            Page3.Visibility = Visibility.Collapsed;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            Page1.Visibility = Visibility.Collapsed;
            Page2.Visibility = Visibility.Visible;
        }

        private void WebBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if ((e.Uri.AbsolutePath.Contains("/v2/Graph/Welcome")) && (e.Uri.Query.Length > 12))
            {
                Page2.Visibility = Visibility.Collapsed;

                string id = e.Uri.Query.Substring(11);
                Debug.WriteLine($"Account Id is: {id}");

                Settings.Data.AccountId = id;
                Settings.Save();

                Page3.Visibility = Visibility.Visible;
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigate($"{Config.ServerAddress}/v2/Authenticate/Graph");
        }
    }
}
