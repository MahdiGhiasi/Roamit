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
using System.Windows.Threading;

namespace QuickShare.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static NotifyIcon notifyIcon;
        ClipboardManager clipboardManager;

        MainViewModel ViewModel { get; } = new MainViewModel();

        SignInWindow signInWindow;
        SettingsWindow settingsWindow;

        bool isExpired = false;

        DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            this.Opacity = 0;
            ClipboardActivity.ItemsSource = ViewModel.ClipboardActivities;

            InitNotifyIcon();

            System.Windows.Application.Current.Deactivated += Application_Deactivated;

            updateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMinutes(30),
            };
            updateTimer.Tick += UpdateTimer_Tick;

            CheckAccountId(true);
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            try
            {
                await Updater.CheckForUpdates();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occured while checking for updates: {ex.Message}");
            }
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
                Text = "Roamit PC Extension",
            };
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private void ExitContextMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
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

            CheckTrialStatus(true);
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

        DateTime lastCheckedTrialStatus = DateTime.MinValue;
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

                if (((isExpired) && ((DateTime.UtcNow - lastCheckedTrialStatus) > TimeSpan.FromMinutes(5))) ||
                    (!knowTrialStatus))
                {
                    lastCheckedTrialStatus = DateTime.UtcNow;
                    CheckTrialStatus(true); //If not expired, no need to refresh from server.
                }
            }
        }

        DateTime lastSendTime = DateTime.MinValue;
        bool willSendAfterLimit = false;
        private async void SendClipboardItem()
        {
            if (isExpired)
            {
                Debug.WriteLine("Premium expired, won't send clipboard.");
                return;
            }

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
                
                await Service.SendCloudClipboard(Settings.Data.AccountId, text);
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
            if (Settings.Data.AccountId == "")
            {
                if (showWindow)
                {
                    InitSignInWindow();
                    signInWindow.Show();
                }

                NotSignedIn.Visibility = Visibility.Visible;
                ActivityContainer.Visibility = Visibility.Collapsed;

                return false;
            }
            else
            {
                NotSignedIn.Visibility = Visibility.Collapsed;
                ActivityContainer.Visibility = Visibility.Visible;

                TryRegisterForStartup();

                if (!updateTimer.IsEnabled)
                    updateTimer.Start();
                CheckForUpdates();

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
#if !DEBUG
            try
            {
                var startupManager = new StartupManager("Roamit Cloud Clipboard");
                startupManager.AddApplicationToCurrentUserStartup();
            }
            catch
            {
                Debug.WriteLine("Failed to register program to run at startup.");
            }
#endif
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

        private void Upgrade_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("roamit://upgrade");
        }

        private void ClipboardActivity_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClipboardActivity.SelectedItem == null)
                return;

            var item = ClipboardActivity.SelectedItem as ClipboardItem;
            System.Windows.Clipboard.SetText(item.Text);
        }

        bool knowTrialStatus = false;
        private async void CheckTrialStatus(bool refreshStatus)
        {
            if ((knowTrialStatus) && (ViewModel.IsTrial))
            {
                UpdateExpireTimeText();

                if (!refreshStatus)
                    return;
            }

            var status = await Service.GetPremiumStatus(Settings.Data.AccountId);

            if (status == null)
                return;

            if (status.State == AccountPremiumState.PremiumTrial)
            {
                ViewModel.IsTrial = true;
                ViewModel.TrialExpireTime = status.TrialExpireTime;

                TrialExpireNoticeContainer.Visibility = Visibility.Visible;

                UpdateExpireTimeText();
            }
            else
            {
                TrialExpireNoticeContainer.Visibility = Visibility.Collapsed;
                isExpired = false;
            }

            knowTrialStatus = true;
        }

        private void UpdateExpireTimeText()
        {
            if (ViewModel.TrialExpireTime < DateTime.UtcNow)
            {
                isExpired = true;

                TrialExpireTimeText.Visibility = Visibility.Collapsed;
                TrialExpireTime.Text = "EXPIRED";
                return;
            }

            isExpired = false;
            TrialExpireTimeText.Visibility = Visibility.Visible;
            var remainingTime = ViewModel.TrialExpireTime - DateTime.UtcNow;

            if (remainingTime.TotalDays >= 2)
            {
                if (remainingTime.Hours > 1)
                    TrialExpireTime.Text = $"{(int)Math.Floor(remainingTime.TotalDays)} days, {remainingTime.Hours} hours";
                else if (remainingTime.Hours == 1)
                    TrialExpireTime.Text = $"{(int)Math.Floor(remainingTime.TotalDays)} days, 1 hour";
                else
                    TrialExpireTime.Text = $"{(int)Math.Floor(remainingTime.TotalDays)} days";
            }
            else if (remainingTime.TotalDays >= 1)
            {
                TrialExpireTime.Text = "1 day";
            }
            else if (remainingTime.TotalHours >= 1)
            {
                TrialExpireTime.Text = $"{(int)Math.Floor(remainingTime.TotalHours)} hours";
            }
            else if (remainingTime.TotalMinutes >= 1)
            {
                TrialExpireTime.Text = $"{(int)Math.Floor(remainingTime.TotalMinutes)} minutes";
            }
            else
            {
                TrialExpireTime.Text = "a few seconds";
            }
        }
    }
}
