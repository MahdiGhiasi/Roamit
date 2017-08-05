using QuickShare.Desktop.Helpers;
using QuickShare.Desktop.ViewModel;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        SettingsViewModel ViewModel { get; } = new SettingsViewModel(Settings.Data.AccountId);

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SignOutAndExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to sign out?\r\n\r\nYour clipboard content will not be synced to your devices, and the extension will not run on startup anymore.\r\nIf you want to enable Cloud Clipboard again, you'll need to open 'Roamit PC Extension' from start menu and sign in with your Microsoft account.",
                "Roamit", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                Settings.Data.AccountId = "";
                Settings.Save();

#if !DEBUG
                try
                {
                    var startupManager = new StartupManager("Roamit Cloud Clipboard");
                    startupManager.RemoveApplicationFromCurrentUserStartup();
                }
                catch
                {
                    Debug.WriteLine("Failed to unregister program from running at startup.");
                }
#endif

                MainWindow.notifyIcon.Visible = false;

                Application.Current.Shutdown();
            }
        }
    }
}
