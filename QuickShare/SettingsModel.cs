using QuickShare.HelperClasses;
using System.ComponentModel;
using System;
using QuickShare.MicrosoftGraphFunctions;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using QuickShare.HelperClasses.VersionHelpers;
using GoogleAnalytics;

namespace QuickShare
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public SettingsModel()
        {
            CheckTrialStatus();
        }

        public void CheckTrialStatus()
        {
            IsFullVersion = !TrialSettings.IsTrial;
        }

        public string PackageName
        {
            get
            {
                return Package.Current.DisplayName + " for Windows 10";
            }
        }

        public string PackageVersion
        {
            get
            {
                return string.Format("{0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Revision);
            }
        }

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
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "FindOtherDevices", "Enabled").Build());
#endif
                }
                else
                {
                    SecureKeyStorage.DeleteUserId();
                    findOtherDevices = SecureKeyStorage.IsUserIdStored();
                    MainPage.Current.ViewModel.ListManager.RemoveAndroidDevices();
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "FindOtherDevices", "Disabled").Build());
#endif
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

        private Visibility fullVersionBoxVisibility = Visibility.Visible;
        public Visibility FullVersionBoxVisibility
        {
            get { return fullVersionBoxVisibility; }
        }

        private Visibility freeVersionBoxVisibility = Visibility.Collapsed;
        public Visibility FreeVersionBoxVisibility
        {
            get { return freeVersionBoxVisibility; }
        }

        private bool isFullVersion;
        public bool IsFullVersion
        {
            get
            {
                return isFullVersion;
            }
            set
            {
                isFullVersion = value;

                if (isFullVersion)
                {
                    fullVersionBoxVisibility = Visibility.Visible;
                    freeVersionBoxVisibility = Visibility.Collapsed;
                }
                else
                {
                    fullVersionBoxVisibility = Visibility.Collapsed;
                    freeVersionBoxVisibility = Visibility.Visible;
                }

                OnPropertyChanged("FullVersionBoxVisibility");
                OnPropertyChanged("FreeVersionBoxVisibility");
                OnPropertyChanged("IsFullVersion");
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