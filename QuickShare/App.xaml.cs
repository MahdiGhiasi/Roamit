using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace QuickShare
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private BackgroundTaskDeferral _backgroundTaskDeferral;
        private AppServiceConnection _appServiceconnection;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            MahdiGhiasi.Rome.RomePackageManager.Instance.Initialize("com.quickshare.service");
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
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

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
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

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            this._backgroundTaskDeferral = args.TaskInstance.GetDeferral();
            args.TaskInstance.Canceled += OnTaskCanceled;
            var details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (details?.Name == "com.quickshare.service") //Remote Activation
            {
                _appServiceconnection = details.AppServiceConnection;
                _appServiceconnection.RequestReceived += OnRequestReceived;
                _appServiceconnection.ServiceClosed += AppServiceconnection_ServiceClosed;                
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                _backgroundTaskDeferral.Complete();
                _backgroundTaskDeferral = null;
            }
        }

        private void AppServiceconnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            if (_backgroundTaskDeferral != null)
            {
                // Complete the service deferral.
                _backgroundTaskDeferral.Complete();
            }
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message.ContainsKey("Receiver"))
            {
                if (args.Request.Message["Receiver"] as string == "ServerIPFinder")
                {
                    await FileSendReceive.ServerIPFinder.Instance.ReceiveRequest(args.Request);
                }
            }
            else if (args.Request.Message.ContainsKey("Test"))
            {
                string s = args.Request.Message["Test"] as string;

                if (s == null)
                    s = "null";

                ValueSet vs = new ValueSet();
                vs.Add("RecvSuccessful", "RecvSuccessful");
                await args.Request.SendResponseAsync(vs);

                await System.Threading.Tasks.Task.Delay(1500);

                Common.ToastFunctions.SendToast(s);
            }
            else if (args.Request.Message.ContainsKey("TestLongRunning"))
            {
                for (int i = 0; i < 10000; i++)
                {
                    Common.ToastFunctions.SendToast((i * 5).ToString() + " seconds");
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                }
            }


            if (_backgroundTaskDeferral != null)
                _backgroundTaskDeferral.Complete();
        }
    }
}
