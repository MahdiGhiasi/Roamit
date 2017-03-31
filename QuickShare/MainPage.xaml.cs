using QuickShare.FileSendReceive;
using QuickShare.Rome;
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

        RemoteSystem selectedSystem = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await packageManager.InitializeDiscovery();
            devicesList.ItemsSource = packageManager.RemoteSystems;
        }

        private void devicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedSystem = devicesList.SelectedItem as RemoteSystem;
            activeDevice.Content = selectedSystem?.DisplayName.ToUpper();
        }

        private void button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
        }
    }
}
