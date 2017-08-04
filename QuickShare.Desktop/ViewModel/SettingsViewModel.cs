using QuickShare.Desktop.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        string accountId;

        public SettingsViewModel(string _accountId)
        {
            accountId = _accountId;
            FetchDevicesList();
        }

        private async void FetchDevicesList()
        {
            var devices = (await Service.GetDevices(accountId))?.OrderBy(x => x.Name ?? "");

            if (devices == null)
                return;

            foreach (var item in devices)
            {
                if ((item.Name ?? "").ToLower() == CurrentDevice.GetDeviceName().ToLower())
                    continue;

                Devices.Add(new DeviceItem(item.CloudClipboardEnabled)
                {
                    AccountID = item.AccountID,
                    DeviceID = item.DeviceID,
                    Name = item.Name,
                    Type = (item.FormFactor == null) ? DeviceType.Unknown :
                           (item.FormFactor.ToLower() == "phone") ? DeviceType.Phone : DeviceType.PC,
                });
            }
        }

        public string VersionNumber
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public ObservableCollection<DeviceItem> Devices { get; } = new ObservableCollection<DeviceItem>();


        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DeviceItem
    {
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public string Name { get; set; }
        public DeviceType Type { get; set; }

        private bool isActive;
        public bool IsActive
        {
            get
            {
                return isActive;
            }
            set
            {
                isActive = value;
                ActiveChanged(value);
            }
        }

        private async void ActiveChanged(bool value)
        {
            await Service.SetCloudClipboardActivation(AccountID, DeviceID, value);
        }

        public DeviceItem(bool _isActive)
        {
            isActive = _isActive; //Does not call cloud service for initial value.
        }
    }

    public enum DeviceType
    {
        PC,
        Phone,
        Unknown,
    }
}
