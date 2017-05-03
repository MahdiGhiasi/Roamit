using QuickShare.DevicesListManager;
using QuickShare.UWP.Rome;
using System.Collections.ObjectModel;
using System.ComponentModel;

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


        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}