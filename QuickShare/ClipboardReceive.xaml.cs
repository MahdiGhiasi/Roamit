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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Guid guid = Guid.Parse(e.Parameter as string);

            DataStorageProviders.HistoryManager.Open();
            var item = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();

            if (!(item.Data is ReceivedText))
                throw new Exception("Invalid received item type.");

            DataStorageProviders.TextReceiveContentManager.Open();
            viewModel["ClipboardContent"] = DataStorageProviders.TextReceiveContentManager.GetItemContent(guid);
            DataStorageProviders.TextReceiveContentManager.Close();

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

        private void circleReverseStoryboard_Completed(object sender, object e)
        {
            Application.Current.Exit();
        }
    }
}
