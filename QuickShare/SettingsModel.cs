using QuickShare.HelperClasses;
using System.ComponentModel;
using System;
using QuickShare.MicrosoftGraphFunctions;

namespace QuickShare
{
    public class SettingsModel : INotifyPropertyChanged
    {
        private bool findOtherDevices = SecureKeyStorage.IsUserIdStored();
        public bool FindOtherDevices
        {
            get { return findOtherDevices; }
            set
            {
                if (value == true)
                {
                    FindOtherDevicesEnabled = false;
                    FindOtherDevicesProgressRingActive = true;
                    ActivateFindingOtherDevices();
                }
                else
                {
                    SecureKeyStorage.DeleteUserId();
                    findOtherDevices = SecureKeyStorage.IsUserIdStored();
                    MainPage.Current.ViewModel.ListManager.RemoveAndroidDevices();
                }
            }
        }

        private bool findOtherDevicesEnabled = true;
        public bool FindOtherDevicesEnabled
        {
            get
            {
                return findOtherDevicesEnabled;
            }
            set
            {
                findOtherDevicesEnabled = value;
                OnPropertyChanged("FindOtherDevicesEnabled");
            }
        }


        private async void ActivateFindingOtherDevices()
        {
            var graph = new Graph(await MSAAuthenticator.GetAccessTokenAsync("User.Read"));
            var userId = await graph.GetUserUniqueIdAsync();
            SecureKeyStorage.SetUserId(userId);

            findOtherDevices = true;
            OnPropertyChanged("FindOtherDevices");

            FindOtherDevicesProgressRingActive = false;
            FindOtherDevicesEnabled = true;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            MainPage.Current.DiscoverOtherDevices(true);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private bool findOtherDevicesProgressRingActive = false;
        public bool FindOtherDevicesProgressRingActive
        {
            get
            {
                return findOtherDevicesProgressRingActive;
            }
            private set
            {
                findOtherDevicesProgressRingActive = value;
                OnPropertyChanged("FindOtherDevicesProgressRingActive");
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