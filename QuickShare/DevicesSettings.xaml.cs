using QuickShare.Classes;
using QuickShare.Common;
using QuickShare.Common.Service;
using QuickShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DevicesSettings : Page
    {
        public DevicesSettingsViewModel Model { get; }

        public DevicesSettings()
        {
            this.InitializeComponent();

            Model = new DevicesSettingsViewModel(SecureKeyStorage.GetAccountId());
        }

        private async void RemoveDevice_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var item = ((Control)sender).Tag as DeviceItem;

            var dlg = new MessageDialog("If you still have Roamit installed on this device, use the 'Log out' option on Roamit settings of the device itself instead of this.\r\n" + 
                "Otherwise it'll re-register once you open Roamit on that device again.", 
                "Are you sure you want to remove this device?");

            dlg.Commands.Add(new UICommand
            {
                Label = "Yes",
                Id = 0,
            });
            dlg.Commands.Add(new UICommand
            {
                Label = "No",
                Id = 1,
            });
            dlg.DefaultCommandIndex = 0;
            dlg.CancelCommandIndex = 1;

            var result = await dlg.ShowAsync();

            if ((result.Id as int?) == 0)
            {
                if (await Device.RemoveDevice(item.AccountID, item.DeviceID))
                {
                    Model.Devices.Remove(item);
                }
                else
                {
                    await (new MessageDialog("Couldn't remove device. Please try again later.")).ShowAsync();
                }
            }
        }
    }
}
