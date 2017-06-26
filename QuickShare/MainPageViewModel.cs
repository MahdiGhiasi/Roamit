using QuickShare.DevicesListManager;
using QuickShare.UWP.Rome;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml;

namespace QuickShare
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public DevicesListManager.DevicesListManager ListManager { get; } = new DevicesListManager.DevicesListManager("", new RemoteSystemNormalizer());

        public MainPageViewModel()
        {
            ListManager.PropertyChanged += ListManager_PropertyChanged;
            DevicesList = ListManager.RemoteSystems;
        }

        private void ListManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedRemoteSystem")
                SelectedRemoteSystem = ListManager.SelectedRemoteSystem;
        }

        ObservableCollection<NormalizedRemoteSystem> devicesList;
        public ObservableCollection<NormalizedRemoteSystem> DevicesList
        {
            get { return devicesList; }
            set
            {
                devicesList = value;
                OnPropertyChanged("DevicesList");
            }
        }

        private NormalizedRemoteSystem selectedRemoteSystem;
        public NormalizedRemoteSystem SelectedRemoteSystem
        {
            get { return selectedRemoteSystem; }
            set
            {
                selectedRemoteSystem = value;
                OnPropertyChanged("SelectedRemoteSystem");
            }
        }

        private string caption = "";
        public string Caption
        {
            get { return caption; }
            set
            {
                if (value.Length == 0)
                    caption = "";
                else
                    caption = value + " - ‌"; //NOTE: This includes a NimFasele in the end of string.
                OnPropertyChanged("Caption");
            }
        }

        private Visibility backButtonPlaceholderVisibility;
        public Visibility BackButtonPlaceholderVisibility
        {
            get { return backButtonPlaceholderVisibility; }
            set
            {
                backButtonPlaceholderVisibility = value;
                OnPropertyChanged("BackButtonPlaceholderVisibility");
            }
        }

        private Visibility upgradeButtonVisibility = Visibility.Visible;
        public Visibility UpgradeButtonVisibility
        {
            get { return upgradeButtonVisibility; }
            set
            {
                upgradeButtonVisibility = value;
                OnPropertyChanged("UpgradeButtonVisibility");
            }
        }

        private Visibility signInNoticeVisibility = Visibility.Collapsed;
        public Visibility SignInNoticeVisibility
        {
            get { return signInNoticeVisibility; }
            set
            {
                signInNoticeVisibility = value;
                OnPropertyChanged("SignInNoticeVisibility");
                OnPropertyChanged("OverlayVisibility");
            }
        }

        public Visibility OverlayVisibility
        {
            get
            {
                if (signInNoticeVisibility == Visibility.Visible)
                    return Visibility.Visible;

                return Visibility.Collapsed;
            }
        }

        private bool isContentFrameEnabled = false;
        public bool IsContentFrameEnabled
        {
            get { return ListManager.SelectedRemoteSystem != null; }
        }

        public void RefreshIsContentFrameEnabled()
        {
            OnPropertyChanged("IsContentFrameEnabled");
        }

        private bool isAcrylicEnabled = false;
        public bool IsAcrylicEnabled
        {
            get { return isAcrylicEnabled; }
            set
            {
                isAcrylicEnabled = value;
                OnPropertyChanged("IsAcrylicEnabled");
                OnPropertyChanged("CustomTopBarVisibility");
                OnPropertyChanged("FramePadding");
            }
        }

        public Visibility CustomTopBarVisibility
        {
            get
            {
                return IsAcrylicEnabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Thickness FramePadding
        {
            get
            {
                return IsAcrylicEnabled ? new Thickness(0, 30, 0, 0) : new Thickness(0, 0, 0, 0);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}