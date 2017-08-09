using QuickShare.Common;
using QuickShare.DataStore;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using GoogleAnalytics;
using QuickShare.Classes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ClipboardReceive : Page
    {
        private ObservableDictionary viewModel = new ObservableDictionary();
        bool isApplicationWindowActive = true;
        bool pendingPaste = false;

        public ClipboardReceive()
        {
            this.InitializeComponent();
            viewModel["WaitingToActivateVisibility"] = Visibility.Visible;
            viewModel["MainVisibility"] = Visibility.Collapsed;
        }

        public ObservableDictionary ViewModel
        {
            get { return this.viewModel; }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter as string == "CLOUD_CLIPBOARD")
            {
                string content = Windows.Storage.ApplicationData.Current.LocalSettings.Values["CloudClipboardText"].ToString();
                viewModel["ClipboardContent"] = content;

                //Re-register the notification
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["LastToast"] = "";
                CloudClipboardHandler.ReceiveRequest(new Dictionary<string, object>
                {
                    {"Data", content},
                });

#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("CloudClipboard", "NotificationTapped").Build());
#endif
            }
            else
            {
                Guid guid = Guid.Parse(e.Parameter as string);

                await DataStorageProviders.HistoryManager.OpenAsync();
                var item = DataStorageProviders.HistoryManager.GetItem(guid);
                DataStorageProviders.HistoryManager.Close();

                if (!(item.Data is ReceivedText))
                    throw new Exception("Invalid received item type.");

                await DataStorageProviders.TextReceiveContentManager.OpenAsync();
                viewModel["ClipboardContent"] = DataStorageProviders.TextReceiveContentManager.GetItemContent(guid);
                DataStorageProviders.TextReceiveContentManager.Close();

#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Clipboard", "NotificationTapped").Build());
#endif
            }

            Window.Current.Activated += Window_Activated;

            HandleClipboardChanged();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.Activated -= Window_Activated;

            base.OnNavigatingFrom(e);
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            isApplicationWindowActive = (e.WindowActivationState != CoreWindowActivationState.Deactivated);
            if (pendingPaste)
            {
                // The clipboard was updated while the sample was in the background. If the sample is now in the foreground, 
                // handle the new content. 
                HandleClipboardChanged();
            }
        }

        private void HandleClipboardChanged()
        {
            if (isApplicationWindowActive)
            {
                PasteToClipboard();
            }
            else
            {
                pendingPaste = true;
            }
        }

        private void PasteToClipboard()
        {
            try
            {
                DataPackage content = new DataPackage();
                content.SetText(viewModel["ClipboardContent"] as string);

                Clipboard.SetContent(content);
            }
            catch (UnauthorizedAccessException ex)
            {
                //Clipboard access is denied. will try again when app activated.
                pendingPaste = true;
                return;
            }

            Debug.WriteLine($"clipboard set to {viewModel["ClipboardContent"] as string}");

            viewModel["WaitingToActivateVisibility"] = Visibility.Collapsed;
            viewModel["MainVisibility"] = Visibility.Visible;

            circleReverseStoryboard.Begin();
        }

        private void CircleReverseStoryboard_Completed(object sender, object e)
        {
            if ((Frame.BackStackDepth > 0) && (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop))
            {
                Frame.GoBack();
            }
            else
            {
                Application.Current.Exit();
            }
        }

        private void WaitingToActivate_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            HandleClipboardChanged();
        }
    }
}
