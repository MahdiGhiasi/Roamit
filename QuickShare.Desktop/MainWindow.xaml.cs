using QuickShare.Desktop.Helpers;
using QuickShare.Desktop.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickShare.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NotifyIcon notifyIcon;
        ClipboardManager clipboardManager;

        MainViewModel ViewModel { get; } = new MainViewModel();

        SignInWindow signInWindow;
        SettingsWindow settingsWindow;

        public MainWindow()
        {
            InitializeComponent();

            this.Opacity = 0;
            this.Visibility = Visibility.Hidden;
            ClipboardActivity.ItemsSource = ViewModel.ClipboardActivities;

            InitNotifyIcon();

            System.Windows.Application.Current.Deactivated += Application_Deactivated;

            CheckAccountId(true);
        }

        private void InitNotifyIcon()
        {
            var contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.Add("&Open", OpenContextMenuItem_Click);
            contextMenu.MenuItems.Add("&Settings", SettingsContextMenuItem_Click);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("E&xit", ExitContextMenuItem_Click);

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = Properties.Resources.icon_white,
                ContextMenu = contextMenu,
            };
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private void ExitContextMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void SettingsContextMenuItem_Click(object sender, EventArgs e)
        {
            Settings_Click(this, new RoutedEventArgs());
        }

        private void OpenContextMenuItem_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }

        #region Stuff related to hiding window when clicked away
        private void Window_Activated(object sender, EventArgs e)
        {
            System.Windows.Input.Mouse.Capture(this, System.Windows.Input.CaptureMode.SubTree);
        }

        double posX, posY;
        DateTime lastTimeLostFocus = DateTime.MinValue;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (posX < 0 || posX > this.Width || posY < 0 || posY > this.Height)
                HideWindow();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point p = e.GetPosition(this);

            posX = p.X; // private double posX is a class member
            posY = p.Y; // private double posY is a class member
        }

        private void Application_Deactivated(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void HideWindow()
        {
            lastTimeLostFocus = DateTime.UtcNow;
            this.Visibility = Visibility.Hidden;
        }
        #endregion

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else if ((DateTime.UtcNow -lastTimeLostFocus) > TimeSpan.FromSeconds(1))
                {
                    ShowWindow();
                }
            }
        }

        private void ShowWindow()
        {
            SetWindowPosition();

            this.Visibility = Visibility.Visible;
            this.Activate();
        }

        private void SetWindowPosition()
        {
            Screen.PrimaryScreen.GetScaleFactors(out double scaleFactorX, out double scaleFactorY);

            var taskbarInfo = new Taskbar(); // taskbarInfo is not DPI aware (returns values in real pixels, not effective pixels)

            switch (taskbarInfo.Position)
            {
                case TaskbarPosition.Left:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX);
                    this.Top = Screen.PrimaryScreen.Bounds.Height - this.Height;
                    break;
                case TaskbarPosition.Top:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX) - this.Width;
                    this.Top = (taskbarInfo.Size.Height / scaleFactorY);
                    break;
                case TaskbarPosition.Right:
                    this.Left = Screen.PrimaryScreen.Bounds.Width - this.Width - (taskbarInfo.Size.Width / scaleFactorX);
                    this.Top = Screen.PrimaryScreen.Bounds.Height - this.Height;
                    break;
                case TaskbarPosition.Bottom:
                default:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX) - this.Width;
                    this.Top = (taskbarInfo.Location.Y / scaleFactorY) - this.Height;
                    break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Initialize the clipboard now that we have a window soruce to use
            clipboardManager = new ClipboardManager(this);
            clipboardManager.ClipboardChanged += ClipboardChanged;
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            // Handle your clipboard update here, debug logging example:
            if (System.Windows.Clipboard.ContainsText())
            {
                string text = System.Windows.Clipboard.GetText();

                ClipboardActivity.Visibility = Visibility.Visible;

                if ((ViewModel.ClipboardActivities.Count > 0) &&
                    (ViewModel.ClipboardActivities.First().DisplayText == text))
                    return;

                ViewModel.ClipboardActivities.Insert(0, new ClipboardItem(text));

                SendClipboardItem();
            }
        }

        DateTime lastSendTime = DateTime.MinValue;
        bool willSendAfterLimit = false;
        private async void SendClipboardItem()
        {
            if (willSendAfterLimit)
                return;

            try
            {
                var elapsed = (DateTime.UtcNow - lastSendTime);
                if (elapsed < TimeSpan.FromSeconds(11))
                {
                    Debug.WriteLine("Waiting...");
                    willSendAfterLimit = true;
                    await Task.Delay(TimeSpan.FromSeconds(11) - elapsed);
                    willSendAfterLimit = false;
                }

                string text = ViewModel.ClipboardActivities[0].Text;
                Debug.WriteLine("Sending...");

                lastSendTime = DateTime.UtcNow;
                using (var httpClient = new HttpClient())
                {
                    string deviceName = System.Net.Dns.GetHostName(); //Environment.MachineName;

                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("accountId", Properties.Settings.Default.AccountId),
                        new KeyValuePair<string, string>("senderName", deviceName),
                        new KeyValuePair<string, string>("text", text),
                    });
                    var response = await httpClient.PostAsync($"{Config.ServerAddress}/v2/Graph/SendCloudClipboard", formContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(responseText);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to send: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendClipboardItem exception: {ex.Message}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetWindowPosition();
            this.Opacity = 1;
            this.Visibility = Visibility.Hidden;
        }

        private bool CheckAccountId(bool showWindow)
        {
            if (Properties.Settings.Default.AccountId == "")
            {
                InitSignInWindow();

                if (showWindow)
                    signInWindow.Show();

                NotSignedIn.Visibility = Visibility.Visible;
                ActivityContainer.Visibility = Visibility.Collapsed;

                return false;
            }
            else
            {
                NotSignedIn.Visibility = Visibility.Collapsed;
                ActivityContainer.Visibility = Visibility.Visible;

                TryRegisterForStartup();

                return true;
            }
        }

        private void InitSignInWindow()
        {
            signInWindow = new SignInWindow();
            signInWindow.Closed += SignInWindow_Closed;
        }

        private void SignInWindow_Closed(object sender, EventArgs e)
        {
            signInWindow.Closed -= SignInWindow_Closed;
            signInWindow = null;

            if (CheckAccountId(false))
            {
                ViewModel.ClipboardActivities.Clear();
                ClipboardActivity.Visibility = Visibility.Collapsed;

                notifyIcon.ShowBalloonTip(int.MaxValue, "Roamit Cloud Clipboard is running in the background.",
                    "You can check the status and change settings by clicking the Roamit icon in the system tray.", ToolTipIcon.None);

                TryRegisterForStartup();
            }
        }

        private void TryRegisterForStartup()
        {
            try
            {
                var startupManager = new StartupManager("Roamit Cloud Clipboard");
                startupManager.AddApplicationToCurrentUserStartup();
            }
            catch
            {
                Debug.WriteLine("Failed to register program to run at startup.");
            }
        }

        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("roamit://");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
                InitSettingsWindow();

            settingsWindow.Show();
            settingsWindow.Activate();
        }

        private void InitSettingsWindow()
        {
            settingsWindow = new SettingsWindow();
            settingsWindow.Closed += Settings_Closed;
        }

        private void Settings_Closed(object sender, EventArgs e)
        {
            settingsWindow.Closed -= SignInWindow_Closed;
            settingsWindow = null;
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            if (signInWindow == null)
                InitSignInWindow();

            signInWindow.Show();
            signInWindow.Activate();
        }

        private void ClipboardActivity_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClipboardActivity.SelectedItem == null)
                return;

            var item = ClipboardActivity.SelectedItem as ClipboardItem;
            System.Windows.Clipboard.SetText(item.Text);
        }
    }
}
