using MahdiGhiasi.Rome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.RemoteSystems;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        RomePackageManager packageManager = RomePackageManager.Instance;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await packageManager.InitializeDiscovery();
            DevicesList.ItemsSource = packageManager.RemoteSystems;
        }

        private async void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var a = 2;
        }

        private async void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var ips = FileSendReceive.ServerIPFinder.FindMyIPAddresses();

            string s = "";
            foreach (var item in ips)
            {
                s += item + " , ";
            }

            ipText.Text = s;
        }

        private async void Button_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".zip");

            var file = await picker.PickSingleFileAsync();

            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            //bytes = buffer.ToArray();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void ContinousNotifications_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DevicesList.SelectedItem == null)
                return;

            var rs = DevicesList.SelectedItem as RemoteSystem;
            var result = await packageManager.Connect(rs, true);
            DevicesList.SelectedItem = null;

            string s = "";
            for (int i = 0; i < 10000; i++)
            {
                s += "1";
            }

            if (result == Windows.ApplicationModel.AppService.AppServiceConnectionStatus.Success)
            {
                for (int i = 0; i < 100000; i++)
                {
                    ValueSet vs = new ValueSet();
                    vs.Add("Test", i.ToString() + " :: " + DateTime.Now.ToString());
                    //vs.Add("Data", s);

                    System.Diagnostics.Debug.WriteLine("Sending #" + i.ToString());

                    var response = await packageManager.Send(vs);
                    if ((response == null) || (response.Message == null) || (!response.Message.ContainsKey("RecvSuccessful")))
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to send #" + i.ToString());
                        i -= 1;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Sent #" + i.ToString());
                    }
                    await Task.Delay(2000);
                }
            }
        }

        private async void OneLongRunning_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DevicesList.SelectedItem == null)
                return;

            var rs = DevicesList.SelectedItem as RemoteSystem;
            var result = await packageManager.Connect(rs, true);
            DevicesList.SelectedItem = null;

            if (result == Windows.ApplicationModel.AppService.AppServiceConnectionStatus.Success)
            {
                ValueSet vs = new ValueSet();
                vs.Add("TestLongRunning", "TestLongRunning");

                var response = await packageManager.Send(vs);
                if ((response == null) || (response.Message == null) || (!response.Message.ContainsKey("RecvSuccessful")))
                {
                    System.Diagnostics.Debug.WriteLine("Failed to send");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Sent");
                }
            }
        }

        private async void CreateFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            var myfolder = await DownloadsFolder.CreateFolderAsync("QuickShare");

            futureAccessList.Clear();
            futureAccessList.Add(myfolder);
        }

        private void CreateFile_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            var folder = futureAccessList.
        }
    }
}
