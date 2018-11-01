using QuickShare.Classes;
using System.ComponentModel;
using System;
using QuickShare.MicrosoftGraphFunctions;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using GoogleAnalytics;
using QuickShare.Common;
using System.Linq;
using System.Diagnostics;
using QuickShare.HelperClasses.Version;
using QuickShare.HelperClasses;
using Windows.Storage;
using Windows.UI.Core;
using System.Collections.Generic;
using QuickShare.Common.Classes;

namespace QuickShare.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        string deviceId = "";
        
        public SettingsViewModel()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("SendCloudClipboard"))
            {
                if (bool.TryParse(ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"].ToString(), out bool scc))
                    sendCloudClipboard = scc;
            }

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("TypeBasedDownloadFolder"))
                ApplicationData.Current.LocalSettings.Values["TypeBasedDownloadFolder"] = false;
            typeBasedDownloadFolderToggle = (ApplicationData.Current.LocalSettings.Values["TypeBasedDownloadFolder"] as bool?) ?? false;

            InitDownloadLocation();

            RetrieveCloudClipboardActivationStatus();
            RetrieveCloudServiceUserName();
        }

        private async void RetrieveCloudServiceUserName()
        {
            if (!SecureKeyStorage.IsAccountIdStored())
                return;

            UserName = await Common.Service.CloudClipboardService.GetUserName(SecureKeyStorage.GetAccountId());

            if (string.IsNullOrWhiteSpace(UserName))
                UserName = "User";
        }

        private async void InitDownloadLocation()
        {
            DefaultDownloadLocation = (await DownloadFolderHelper.GetDefaultDownloadFolderAsync()).Path;
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

        public Visibility SendCloudClipboardVisibility
        {
            get { return (PCExtensionHelper.IsSupported) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsCloudServiceLoggedIn
        {
            get => SecureKeyStorage.IsTokenStored() && SecureKeyStorage.IsAccountIdStored();
        }

        private bool receiveCloudClipboard = true;
        public bool ReceiveCloudClipboard
        {
            get { return receiveCloudClipboard; }
            set
            {
                SetReceiveCloudClipboardValue(value);
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "ReceiveCloudClipboard", value ? "activated" : "deactivated").Build());
#endif
            }
        }

        private async void SetReceiveCloudClipboardValue(bool value)
        {
            ReceiveCloudClipboardEnabled = false;
            ReceiveCloudClipboardProgressRingActive = true;

            await Common.Service.CloudClipboardService.SetCloudClipboardActivation(SecureKeyStorage.GetAccountId(), deviceId, value);

            receiveCloudClipboard = value;
            OnPropertyChanged("ReceiveCloudClipboard");

            ReceiveCloudClipboardEnabled = true;
            ReceiveCloudClipboardProgressRingActive = false;

        }

        private async void RetrieveCloudClipboardActivationStatus()
        {
            if (!SecureKeyStorage.IsAccountIdStored())
                return;

            ReceiveCloudClipboardEnabled = false;
            ReceiveCloudClipboardProgressRingActive = true;

            try
            {
                var result = await Common.Service.CloudClipboardService.GetDevices(SecureKeyStorage.GetAccountId());
                var deviceName = (new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation()).FriendlyName;

                Debug.WriteLine($"Current device name is '{deviceName}'");

                var currentDevice = result.FirstOrDefault(x => (x.Name ?? "").ToLower() == deviceName.ToLower());

                if (currentDevice == null)
                    return;

                deviceId = currentDevice.DeviceID;
                receiveCloudClipboard = currentDevice.CloudClipboardEnabled;
                OnPropertyChanged("ReceiveCloudClipboard");
                ReceiveCloudClipboardEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in RetrieveCloudClipboardActivationStatus: {ex.Message}");
            }
            finally
            {
                ReceiveCloudClipboardProgressRingActive = false;
            }
        }

        private bool receiveCloudClipboardEnabled = true;
        public bool ReceiveCloudClipboardEnabled
        {
            get
            {
                return receiveCloudClipboardEnabled && IsCloudServiceLoggedIn;
            }
            set
            {
                receiveCloudClipboardEnabled = value;
                OnPropertyChanged("ReceiveCloudClipboardEnabled");
            }
        }

        private bool receiveCloudClipboardProgressRingActive = false;
        public bool ReceiveCloudClipboardProgressRingActive
        {
            get
            {
                return receiveCloudClipboardProgressRingActive;
            }
            private set
            {
                receiveCloudClipboardProgressRingActive = value;
                OnPropertyChanged("ReceiveCloudClipboardProgressRingActive");
            }
        }

        private bool sendCloudClipboard = false;
        public bool SendCloudClipboard
        {
            get { return sendCloudClipboard; }
            set
            {
                SetSendCloudClipboardValue(value);
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "SendCloudClipboard", value ? "activated" : "deactivated").Build());
#endif
            }
        }

        private async void SetSendCloudClipboardValue(bool value)
        {
            SendCloudClipboardProgressRingActive = true;
            SendCloudClipboardEnabled = false;

            sendCloudClipboard = value;
            ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"] = value.ToString();
            OnPropertyChanged("SendCloudClipboard");

            if (value == true)
                await PCExtensionHelper.StartPCExtension();
            else
                await PCExtensionHelper.StopPCExtensionIfRunning();

            SendCloudClipboardProgressRingActive = false;
            SendCloudClipboardEnabled = true;
        }

        private bool sendCloudClipboardEnabled = true;
        public bool SendCloudClipboardEnabled
        {
            get
            {
                return sendCloudClipboardEnabled && IsCloudServiceLoggedIn;
            }
            set
            {
                sendCloudClipboardEnabled = value;
                OnPropertyChanged("SendCloudClipboardEnabled");
            }
        }

        private bool sendCloudClipboardProgressRingActive = false;
        public bool SendCloudClipboardProgressRingActive
        {
            get
            {
                return sendCloudClipboardProgressRingActive;
            }
            private set
            {
                sendCloudClipboardProgressRingActive = value;
                OnPropertyChanged("SendCloudClipboardProgressRingActive");
            }
        }

        private string defaultDownloadLocation = "";
        public string DefaultDownloadLocation
        {
            get
            {
                return defaultDownloadLocation;
            }
            set
            {
                defaultDownloadLocation = value;
                OnPropertyChanged("DefaultDownloadLocation");
            }
        }

        private bool typeBasedDownloadFolderToggle = false;
        public bool TypeBasedDownloadFolderToggle
        {
            get
            {
                return typeBasedDownloadFolderToggle;
            }
            set
            {
                typeBasedDownloadFolderToggle = value;
                ApplicationData.Current.LocalSettings.Values["TypeBasedDownloadFolder"] = value;
                OnPropertyChanged("TypeBasedDownloadFolderToggle");
#if !DEBUG
                    App.Tracker.Send(HitBuilder.CreateCustomEvent("Settings", "TypeBasedDownloadFolderToggle", value ? "activated" : "deactivated").Build());
#endif
            }
        }

        private string userName = "";
        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                userName = value;

                OnPropertyChanged("UserName");
                OnPropertyChanged("SignedInFullName");
                OnPropertyChanged("IsSignedInFullNameAvailable");
            }
        }

        public string SignedInFullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UserName))
                    return "...";
                return UserName;
            }
        }

        public bool IsSignedInFullNameAvailable
        {
            get => !string.IsNullOrEmpty(UserName);
        }

        public void RefreshCloudClipboardBindings()
        {
            OnPropertyChanged("SendCloudClipboardEnabled");
            OnPropertyChanged("ReceiveCloudClipboardEnabled");
            OnPropertyChanged("IsCloudServiceLoggedIn");
        }

        public DownloadGroupByItem GroupReceivedBySelectedItem
        {
            get
            {
                return DownloadGroupByHelper.GetState();
            }
            set
            {
                DownloadGroupByHelper.SetState(value);
            }
        }

        public IEnumerable<DownloadGroupByItem> GroupReceivedByItems
        {
            get
            {
                return DownloadGroupByItem.GroupItems;
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