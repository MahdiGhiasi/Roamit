using GoogleAnalytics;
using Microsoft.QueryStringDotNET;
using Newtonsoft.Json;
using QuickShare.Common;
using QuickShare.DataStore;
using QuickShare.FileTransfer;
using QuickShare.Classes;
using QuickShare.TextTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QuickShare.HelperClasses;
using QuickShare.ViewModels.ShareTarget;
using QuickShare.HelperClasses.Version;

namespace QuickShare
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
#if !DEBUG
        public static Tracker Tracker;
#endif
        public static DateTime? LaunchTime { get; set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += App_UnhandledException;

            LaunchTime = DateTime.Now;

            UWP.Rome.RomePackageManager.Instance.Initialize("com.roamit.service");
            DataStore.DataStorageProviders.Init(Windows.Storage.ApplicationData.Current.LocalFolder.Path);

#if !DEBUG
            AnalyticsManager.Current.IsDebug = false; //use only for debugging, returns detailed info on hits sent to analytics servers
            AnalyticsManager.Current.ReportUncaughtExceptions = true; //catch unhandled exceptions and send the details
            AnalyticsManager.Current.AutoAppLifetimeMonitoring = true; //handle suspend/resume and empty hit batched hits on suspend

            Tracker = AnalyticsManager.Current.CreateTracker(Common.Secrets.GoogleAnalyticsId);
            Tracker.AppName = "Roamit-Windows10";
#endif
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogExceptionMessage(e.Message + "\r\n\r\n" + e.Exception.ToString());
        }

        private async void LogExceptionMessage(string msg)
        {
            try
            {
                var message = new MessageDialog(msg, "Unhandled exception occured.");
                await message.ShowAsync();
            }
            catch { }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if ((ApplicationData.Current.LocalSettings.Values.ContainsKey("FirstRun")) && (ApplicationData.Current.LocalSettings.Values["FirstRun"].ToString() == "false"))
            {
                InitApplication(e, typeof(MainPage));
            }
            else
            {
                LaunchTime = null;
                InitApplication(e, typeof(Intro));
            }
        }

        private void InitApplication(LaunchActivatedEventArgs e, Type defaultPage)
        {
            Debug.WriteLine("Launched.");
#if DEBUG && false
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e?.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e?.PrelaunchActivated != true)
            {
                if ((rootFrame.Content == null) && (defaultPage != null))
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(defaultPage, e?.Arguments);
                }
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(330, 550));

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName, e.Exception);
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            Debug.WriteLine("Activated.");

            Frame rootFrame = Window.Current.Content as Frame;

            bool isJustLaunched = (rootFrame == null);

            if (e is ToastNotificationActivatedEventArgs)
            {
                var toastActivationArgs = e as ToastNotificationActivatedEventArgs;

                // Parse the query string
                QueryString args = QueryString.Parse(toastActivationArgs.Argument);

                HistoryRow hr;
                switch (args["action"])
                {
                    case "cloudClipboard":
                        LaunchRootFrameIfNecessary(ref rootFrame, false);
                        rootFrame.Navigate(typeof(ClipboardReceive), "CLOUD_CLIPBOARD");
                        break;
                    case "clipboardReceive":
                        LaunchRootFrameIfNecessary(ref rootFrame, false);
                        rootFrame.Navigate(typeof(ClipboardReceive), args["guid"]);
                        break;
                    case "fileProgress":
                        LaunchRootFrameIfNecessary(ref rootFrame, true);
                        if (rootFrame.Content is MainPage)
                            break;
                        rootFrame.Navigate(typeof(MainPage));
                        break;
                    case "fileFinished":
                        LaunchRootFrameIfNecessary(ref rootFrame, true);

                        //TODO: Open history page

                        break;
                    case "openFolder":
                        hr = await GetHistoryItemGuid(Guid.Parse(args["guid"]));
                        await LaunchOperations.LaunchFolderFromPathAsync((hr.Data as ReceivedFileCollection).StoreRootPath);
                        if (isJustLaunched)
                            Application.Current.Exit();
                        break;
                    case "openFolderSingleFile":
                        hr = await GetHistoryItemGuid(Guid.Parse(args["guid"]));
                        await LaunchOperations.LaunchFolderFromPathAndSelectSingleItemAsync((hr.Data as ReceivedFileCollection).Files[0].StorePath, (hr.Data as ReceivedFileCollection).Files[0].Name);
                        if (isJustLaunched)
                            Application.Current.Exit();
                        break;
                    case "openSingleFile":
                        hr = await GetHistoryItemGuid(Guid.Parse(args["guid"]));
                        await LaunchOperations.LaunchFileFromPathAsync((hr.Data as ReceivedFileCollection).Files[0].StorePath, (hr.Data as ReceivedFileCollection).Files[0].Name);
                        if (isJustLaunched)
                            Application.Current.Exit();
                        break;
                    default:
                        break;
                }

            }
            else if (e.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs pEventArgs = e as ProtocolActivatedEventArgs;

                if ((pEventArgs.Uri.AbsoluteUri.ToLower() == "roamit://wake") || (pEventArgs.Uri.AbsoluteUri.ToLower() == "roamit://wake/"))
                {
                    Debug.WriteLine("Wake request received");
                    Application.Current.Exit();
                }
                else
                {
                    string clipboardData = ParseFastClipboardUri(pEventArgs.Uri.AbsoluteUri);
                    string launchUriData = ParseLaunchUri(pEventArgs.Uri.AbsoluteUri);
                    bool isUpgrade = ParseUpgrade(pEventArgs.Uri.AbsoluteUri);

                    if (isUpgrade)
                    {
                        if (rootFrame == null)
                        {
                            LaunchRootFrameIfNecessary(ref rootFrame, false);
                            rootFrame.Navigate(typeof(MainPage), "upgrade");
                        }
                        else
                        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            TrialHelper.AskForUpgrade();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        }
                    }
                    else if (clipboardData.Length > 0)
                    {
                        string[] parts = clipboardData.Split('?');
                        var guid = await TextReceiver.QuickTextReceivedAsync(parts[0].DecodeBase64(), parts[1].DecodeBase64());

                        LaunchRootFrameIfNecessary(ref rootFrame, false);
                        rootFrame.Navigate(typeof(ClipboardReceive), guid.ToString());
                    }
                    else if (launchUriData.Length > 0)
                    {
#if !DEBUG
                        App.Tracker.Send(HitBuilder.CreateCustomEvent("ExtensionCalled", "").Build());
#endif

                        string type = ExternalContentHelper.SetUriData(new Uri(launchUriData.DecodeBase64()));
                        
                        SendDataTemporaryStorage.IsSharingTarget = true;

                        if (rootFrame == null)
                        {
                            LaunchRootFrameIfNecessary(ref rootFrame, false);
                            rootFrame.Navigate(typeof(MainPage), new ShareTargetDetails
                            {
                                Type = type,
                            });
                        }
                        else
                        {
                            MainPage.Current.BeTheShareTarget(new ShareTargetDetails
                            {
                                Type = type,
                            });
                        }
                    }
                    else
                    {
                        LaunchRootFrameIfNecessary(ref rootFrame, true);
                    }
                }
            }
            else
            {
                LaunchRootFrameIfNecessary(ref rootFrame, true);
            }

            base.OnActivated(e);
        }

        private bool ParseUpgrade(string s)
        {
            s = s.ToLower();

            if ((s == "roamit://upgrade") || (s == "roamit://upgrade/"))
                return true;
            return false;
        }

        private string ParseFastClipboardUri(string s)
        {
            string fastClipboardUri = "roamit://clipboard/";
            if (s.Length < fastClipboardUri.Length)
                return "";

            var command = s.Substring(0, fastClipboardUri.Length).ToLower();

            return (command == fastClipboardUri) ? s.Substring(fastClipboardUri.Length) : "";
        }

        private string ParseLaunchUri(string s)
        {
            string launchUri = "roamit://remotelaunch/";
            if (s.Length < launchUri.Length)
                return "";

            var command = s.Substring(0, launchUri.Length).ToLower();

            return (command == launchUri) ? s.Substring(launchUri.Length) : "";
        }

        private async Task<HistoryRow> GetHistoryItemGuid(Guid guid)
        {
            HistoryRow hr;
            await DataStorageProviders.HistoryManager.OpenAsync();
            hr = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();
            return hr;
        }

        private void LaunchRootFrameIfNecessary(ref Frame rootFrame, bool launchMainPage)
        {
            LaunchTime = null;
            if (rootFrame == null)
            {
                InitApplication(null, launchMainPage ? typeof(MainPage) : null);
                rootFrame = Window.Current.Content as Frame;
            }
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private AppServiceConnection appServiceConnection;
        private BackgroundTaskDeferral appServiceDeferral;

        private AppServiceConnection messageCarrierAppServiceConnection;
        private BackgroundTaskDeferral messageCarrierAppServiceDeferral;

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (appService?.Name == "com.roamit.notificationservice")
            {
                appServiceDeferral = taskInstance.GetDeferral();
                taskInstance.Canceled += OnAppServicesCanceled;
                appServiceConnection = appService.AppServiceConnection;
                appServiceConnection.RequestReceived += OnAppServiceRequestReceived;
                appServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
            else if (appService?.Name == "com.roamit.messagecarrierservice")
            {
                messageCarrierAppServiceDeferral = taskInstance.GetDeferral();
                taskInstance.Canceled += OnMessageCarrierAppServicesCanceled;
                messageCarrierAppServiceConnection = appService.AppServiceConnection;
                messageCarrierAppServiceConnection.RequestReceived += OnMessageCarrierAppServiceRequestReceived;
                messageCarrierAppServiceConnection.ServiceClosed += MessageCarrierAppServiceConnection_ServiceClosed;
            }
        }

        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            try
            {
                AppServiceDeferral messageDeferral = args.GetDeferral();
                ValueSet message = args.Request.Message;

                if (message["Type"].ToString() == "FileTransferProgress")
                {
                    await DispatcherEx.RunOnCoreDispatcherIfPossible(async () =>
                    {
                        await NotificationHandler.HandleAsync(JsonConvert.DeserializeObject<FileTransferProgressEventArgs>(message["Data"] as string));
                    });
                }
                else if (message["Type"].ToString() == "TextReceive")
                {
                    await DispatcherEx.RunOnCoreDispatcherIfPossible(async () =>
                    {
                        await NotificationHandler.HandleAsync(JsonConvert.DeserializeObject<TextReceiveEventArgs>(message["Data"] as string));
                    });
                }

                ValueSet returnMessage = new ValueSet();
                returnMessage.Add("Status", "OK");
                await args.Request.SendResponseAsync(returnMessage);

                messageDeferral.Complete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unhandled exception in OnAppServiceRequestReceived():");
                Debug.WriteLine(ex.ToString());
                await (new MessageDialog(ex.ToString(), "Unhandled exception in OnAppServiceRequestReceived()")).ShowAsync();
            }
        }

        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            appServiceDeferral?.Complete();
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            appServiceDeferral?.Complete();
        }

        private void MessageCarrierAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            messageCarrierAppServiceDeferral?.Complete();
        }

        private void OnMessageCarrierAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            messageCarrierAppServiceDeferral?.Complete();
        }

        DateTime lastCall = DateTime.MinValue;
        private async void OnMessageCarrierAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            try
            {
                Debug.WriteLine("A message carrier received. Processing...");
                await MainPage.Current.AndroidPackageManager.MessageCarrierReceivedAsync(args.Request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while processing MessageCarrier.");
                Debug.WriteLine(ex.ToString());
            }

            lastCall = DateTime.Now;
            CheckIfIsOver(lastCall);

            deferral.Complete();
        }

        private async void CheckIfIsOver(DateTime callTime)
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
            if (lastCall != callTime)
                return;

            if (!MainPage.Current.AndroidPackageManager.HasWaitingMessageCarrier)
            {
                Debug.WriteLine("We're done here.");
                messageCarrierAppServiceDeferral.Complete();
            }
        }

        public static ShareOperation ShareOperation;
        protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            ShareOperation = args.ShareOperation;
            string type = await ExternalContentHelper.SetData(ShareOperation.Data);

            if (type == "")
            {
                ShareOperation.ReportError("Unknown data type received.");
                return;
            }

            ShareOperation.ReportDataRetrieved();
            SendDataTemporaryStorage.IsSharingTarget = true;

            Frame rootFrame = null;
            LaunchRootFrameIfNecessary(ref rootFrame, false);
            rootFrame.Navigate(typeof(MainPage), new ShareTargetDetails
            {
                Type = type,
            });
        }
    }
}
