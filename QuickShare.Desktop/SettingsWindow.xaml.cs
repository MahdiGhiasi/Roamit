using System;
using System.Collections.Generic;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SignOutAndExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to sign out?\r\n\r\nYour clipboard content will not be synced to your devices. If you want to enable Cloud Clipboard again, you'll need to open 'Roamit Cloud Clipboard' from start menu and sign in with your Microsoft account.",
                "Roamit", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                Properties.Settings.Default.AccountId = "";
                Properties.Settings.Default.Save();
                Application.Current.Shutdown();
            }
        }
    }
}
