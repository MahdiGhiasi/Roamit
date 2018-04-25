using QuickShare.Common.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace QuickShare.ViewModels
{
    public class DevicesSettingsViewModel : INotifyPropertyChanged
    {
        string accountId;

        public ObservableCollection<DeviceItem> Devices { get; } = new ObservableCollection<DeviceItem>();

        public DevicesSettingsViewModel(string _accountId)
        {
            accountId = _accountId;
            FetchDevicesList();
        }

        private async void FetchDevicesList()
        {
            var devices = (await CloudClipboardService.GetDevices(accountId))?.OrderBy(x => x.Name ?? "");

            if (devices == null)
                return;

            foreach (var item in devices)
            {
                if ((item.Name ?? "").ToLower() == (new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation()).FriendlyName.ToLower())
                    continue;

                Devices.Add(new DeviceItem(item));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DeviceItem
    {
        public string AccountID { get; }
        public string DeviceID { get; }
        public string Name { get; }
        public string Kind { get; }

        private bool isReceiveUniversalClipboardActive;
        public bool IsReceiveUniversalClipboardActive
        {
            get
            {
                return isReceiveUniversalClipboardActive;
            }
            set
            {
                isReceiveUniversalClipboardActive = value;
                ActiveChanged(value);
            }
        }

        public bool CanRemoveDevice { get => Kind == null || Kind?.ToLower() == "qs_android"; }

        private async void ActiveChanged(bool value)
        {
            await CloudClipboardService.SetCloudClipboardActivation(AccountID, DeviceID, value);
        }

        public DeviceItem(DeviceInformation item)
        {
            isReceiveUniversalClipboardActive = item.CloudClipboardEnabled; //Does not call cloud service for initial value.
            AccountID = item.AccountID;
            DeviceID = item.DeviceID;
            Name = item.Name;
            Kind = item.FormFactor;
        }
    }

    public enum DeviceType
    {
        PC,
        Phone,
        Unknown,
    }
}
