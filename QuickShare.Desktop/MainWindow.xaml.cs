using CSharpAnalytics;
using QuickShare.Common.Service;
using QuickShare.Desktop.Helpers;
using QuickShare.Desktop.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

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


        double myHeight;
        double myWidth;

#if SQUIRREL
        DispatcherTimer updateTimer;
        DispatcherTimer checkForStoreVersionTimer;
#endif

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitGoogleAnalytics();

            myHeight = this.Height;
            myWidth = this.Width;
            this.Height = 0;
            this.Width = 0;

            this.Opacity = 0;
            ClipboardActivity.ItemsSource = ViewModel.ClipboardActivities;

            InitApp();
        }

        private async void InitApp()
        {
            if ((await PurposeHelper.ConfirmPurpose()) == false)
                return;

            InitNotifyIcon();

            System.Windows.Application.Current.Deactivated += Application_Deactivated;

#if SQUIRREL
            updateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMinutes(30),
            };
            updateTimer.Tick += UpdateTimer_Tick;

            checkForStoreVersionTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(5),
            };
            checkForStoreVersionTimer.Tick += CheckForStoreVersionTimer_Tick;
#endif

            CheckAccountId(true);

#if !SQUIRREL
            ApplicationRestart.RegisterForRestart();
#endif

            if (Properties.Settings.Default.HasLastExceptionMessage)
            {
                var exceptionMessage = Properties.Settings.Default.LastExceptionMessage ?? "null";

                Properties.Settings.Default.HasLastExceptionMessage = false;
                Properties.Settings.Default.LastExceptionMessage = "";
                Properties.Settings.Default.Save();

#if !DEBUG
                AutoMeasurement.Client.TrackEvent("UnhandledException", "Fatal", exceptionMessage);
#endif
            }


#if !SQUIRREL
            Settings.Save();
#endif

            var currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length > 1)
            {
                var otherProcess = processes.Where(p => p.Id != currentProcess.Id && p.MainModule.FileName != currentProcess.MainModule.FileName).FirstOrDefault();

                if (otherProcess != null)
                {
#if SQUIRREL
                    //Store version is running, so I'll open roamit and close myself
                    Process.Start("roamit://");
                    notifyIcon.Visible = false;
                    System.Windows.Application.Current.Shutdown();
#else
                    //Close squirrel version of the app, if running
                    otherProcess.CloseApp();
#endif
                }
            }
        }

#if SQUIRREL
        private void CheckForStoreVersionTimer_Tick(object sender, EventArgs e)
        {
            var currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length > 1)
            {
                var otherProcess = processes.Where(p => p.Id != currentProcess.Id && p.MainModule.FileName != currentProcess.MainModule.FileName).FirstOrDefault();

                if (otherProcess != null)
                {
                    notifyIcon.Visible = false;
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }
#endif

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!e.IsTerminating)
                return;

            notifyIcon.Visible = false;

            Properties.Settings.Default.LastExceptionMessage = e.ExceptionObject.ToString();
            Properties.Settings.Default.HasLastExceptionMessage = true;
            Properties.Settings.Default.Save();

#if SQUIRREL
            ProcessStartInfo Info = new ProcessStartInfo()
            {
                Arguments = "/C choice /C Y /N /D Y /T 5 & START \"\" \"" + Assembly.GetExecutingAssembly().Location + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(Info);
            System.Windows.Application.Current.Shutdown();
#endif
        }

        private static void InitGoogleAnalytics()
        {
#if SQUIRREL
            string versionExtension = "-squirrel";
#elif !DEBUG
            string versionExtension = "-store";
#endif

#if !DEBUG
            AutoMeasurement.Instance = new WpfAutoMeasurement();
            AutoMeasurement.Start(new MeasurementConfiguration(Common.Secrets.GoogleAnalyticsId, "PCExtension", Assembly.GetExecutingAssembly().GetName().Version.ToString() + versionExtension));

            AutoMeasurement.Client.TrackScreenView("MainWindow");
#endif
        }

#if SQUIRREL
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            CheckForUpdates();
        }
#endif

        private async void CheckForUpdates()
        {
#if SQUIRREL
            try
            {
                await Updater.CheckForUpdates();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occured while checking for updates: {ex.Message}");
            }
#endif
        }

        private void InitNotifyIcon()
        {
            var contextMenu = new System.Windows.Forms.ContextMenu();

#if SQUIRREL
            contextMenu.MenuItems.Add("&Open", OpenContextMenuItem_Click);
            contextMenu.MenuItems.Add("&Settings", SettingsContextMenuItem_Click);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("E&xit", ExitContextMenuItem_Click);
#else
            contextMenu.MenuItems.Add("&Open Clipboard Pane", OpenContextMenuItem_Click);
            contextMenu.MenuItems.Add("Open &Roamit", OpenRoamitContextMenuItem_Click);
            contextMenu.MenuItems.Add("&Settings", SettingsContextMenuItem_Click);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add("&Close", CloseRoamitContextMenuItem_Click);
#endif

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = Properties.Resources.icon_white,
                ContextMenu = contextMenu,
#if SQUIRREL
                Text = "Roamit PC Extension",
#else
                Text = "Roamit",
#endif
            };
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }

        private async void CloseRoamitContextMenuItem_Click(object sender, EventArgs e)
        {
            var dialogResult = System.Windows.MessageBox.Show("Do you want to close Roamit?\r\n\r\nYour clipboard will no longer mirrored to your other devices, until you open Roamit again or restart your computer.", "Roamit", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (dialogResult == System.Windows.MessageBoxResult.Yes)
            {
                //Hide app icon
                notifyIcon.Visible = false;

                //Tell UWP app to shut down if is open right now.
                try
                {
                    using (AppServiceConnection connection = new AppServiceConnection
                    {
                        AppServiceName = "com.roamit.pcservice",
                        PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName
                    })
                    {
                        var result = await connection.OpenAsync();
                        if (result == AppServiceConnectionStatus.Success)
                        {
                            ValueSet valueSet = new ValueSet
                            {
                                { "Action", "Die" },
                            };
                            var response = await connection.SendMessageAsync(valueSet);
                        }
                    }
                }
                catch { }

                await Task.Delay(1000);

                //Exit
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void OpenRoamitContextMenuItem_Click(object sender, EventArgs e)
        {
            OpenApp_Click(this, new RoutedEventArgs());
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
                else if ((DateTime.UtcNow - lastTimeLostFocus) > TimeSpan.FromSeconds(0.5))
                {
                    ShowWindow();
                }
            }
        }

        private void ShowWindow()
        {
            this.Height = myHeight;
            this.Width = myWidth;
            SetWindowPosition();

            this.Visibility = Visibility.Visible;
            this.Activate();
        }

        private void SetWindowPosition()
        {
            Screen.PrimaryScreen.GetScaleFactors(out double scaleFactorX, out double scaleFactorY);

            var taskbarInfo = new Taskbar(); // taskbarInfo is not DPI aware (returns values in real pixels, not effective pixels)

            Debug.WriteLine($"Taskbar position is {taskbarInfo.Position.ToString()}");
            Debug.WriteLine($"Taskbar size is {taskbarInfo.Size.Width}, {taskbarInfo.Size.Height}");
            Debug.WriteLine($"Taskbar location is {taskbarInfo.Location.X}, {taskbarInfo.Location.Y}");
            Debug.WriteLine($"Scale factors: X = {scaleFactorX}, Y = {scaleFactorY}");
            Debug.WriteLine($"SystemParameters.PrimaryScreen(Width, Height) = {SystemParameters.PrimaryScreenWidth}, {SystemParameters.PrimaryScreenHeight}");
            Debug.WriteLine($"My size: {this.Width}, {this.Height}");

            switch (taskbarInfo.Position)
            {
                case TaskbarPosition.Left:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX);
                    this.Top = (taskbarInfo.Size.Height / scaleFactorY) - this.Height;
                    break;
                case TaskbarPosition.Top:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX) - this.Width;
                    this.Top = (taskbarInfo.Size.Height / scaleFactorY);
                    break;
                case TaskbarPosition.Right:
                    this.Left = (Screen.PrimaryScreen.Bounds.Width / scaleFactorX) - this.Width - (taskbarInfo.Size.Width / scaleFactorX);
                    this.Top = (taskbarInfo.Size.Height / scaleFactorY) - this.Height;
                    break;
                case TaskbarPosition.Bottom:
                default:
                    this.Left = (taskbarInfo.Size.Width / scaleFactorX) - this.Width;
                    this.Top = (taskbarInfo.Location.Y / scaleFactorY) - this.Height;
                    break;
            }
            Debug.WriteLine($"Window position: {this.Left}, {this.Top}");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Initialize the clipboard now that we have a window soruce to use
            clipboardManager = new ClipboardManager(this);
            clipboardManager.ClipboardChanged += ClipboardChanged;
        }

        DateTime lastCheckedTrialStatus = DateTime.MinValue;
        SemaphoreSlim clipboardChangedSemaphore = new SemaphoreSlim(1, 1);
        private async void ClipboardChanged(object sender, EventArgs e)
        {
            try
            {
                await clipboardChangedSemaphore.WaitAsync();

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
            catch (Exception ex)
            {
#if !DEBUG
                AutoMeasurement.Client.TrackEvent("Exception", "ClipboardChanged", ex.Message);
#endif
            }
            finally
            {
                clipboardChangedSemaphore.Release();
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

                await CloudClipboardService.SendCloudClipboard(Settings.Data.AccountId, text, CurrentDevice.GetDeviceName());
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
#if SQUIRREL
                if (showWindow)
                {
                    InitSignInWindow();
                    signInWindow.Show();
                }

                NotSignedIn.Visibility = Visibility.Visible;
                ActivityContainer.Visibility = Visibility.Collapsed;

#endif
                return false;
            }
            else
            {
                NotSignedIn.Visibility = Visibility.Collapsed;
                ActivityContainer.Visibility = Visibility.Visible;

                TryRegisterForStartup();

#if SQUIRREL
                if (!updateTimer.IsEnabled)
                    updateTimer.Start();
                CheckForUpdates();
#endif

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

                notifyIcon.ShowBalloonTip(int.MaxValue, "Roamit universal clipboard is running in the background.",
                    "You can check the status and change settings by clicking the Roamit icon in the system tray.", ToolTipIcon.None);

                TryRegisterForStartup();
            }
        }

        private async void TryRegisterForStartup()
        {
            try
            {
#if SQUIRREL
                var startupManager = new StartupManager("Roamit Cloud Clipboard");
                startupManager.AddApplicationToCurrentUserStartup();
#else
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("RoamitStartupTask");
                if (startupTask.State != Windows.ApplicationModel.StartupTaskState.Enabled)
                {
                    var state = await startupTask.RequestEnableAsync();
                    if (state == Windows.ApplicationModel.StartupTaskState.DisabledByUser)
                    {
                        notifyIcon?.ShowBalloonTip(int.MaxValue, "Roamit is not allowed to run on startup",
                            "For best universal clipboard experience, please allow Roamit to run on startup from Task Manager", ToolTipIcon.Warning);
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to register program to run at startup.");
#if !SQUIRREL
                notifyIcon?.ShowBalloonTip(int.MaxValue, "Failed to register Roamit to run on startup",
                    ex.Message, ToolTipIcon.Warning);
#endif
            }
        }

        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("roamit://");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
#if SQUIRREL
             if (settingsWindow == null)
                InitSettingsWindow();

            settingsWindow.Show();
            settingsWindow.Activate();
#else
            Process.Start("roamit://settings");
#endif
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (notifyIcon != null)
                notifyIcon.Visible = false;

#if SQUIRREL
            var currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            if (processes.Length > 1)
            {
                var proc = processes.Where(p => p.Id != currentProcess.Id && p.MainModule.FileName != currentProcess.MainModule.FileName).FirstOrDefault();

                if (proc != null)
                {
                    //Store version is present, so I'll stop running at startup

                    try
                    {
                        var startupManager = new StartupManager("Roamit Cloud Clipboard");
                        startupManager.RemoveApplicationFromCurrentUserStartup();
                    }
                    catch
                    {
                        Debug.WriteLine("Failed to unregister program to run at startup.");
                    }
                }
            }
#endif
        }
    }
}
