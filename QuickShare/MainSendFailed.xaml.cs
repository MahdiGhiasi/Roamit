using Newtonsoft.Json;
using QuickShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainSendFailed : Page
    {
        public SendFailedViewModel ViewModel { get; internal set; }

        public MainSendFailed()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel = JsonConvert.DeserializeObject<SendFailedViewModel>(e.Parameter.ToString());
        }

        private void Retry_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SendDataTemporaryStorage.IsSharingTarget)
            {
                if (App.ShareOperation == null)
                {
                    App.Current.Exit();
                }
                else
                {
                    App.ShareOperation.ReportCompleted();
                }
            }
            else
            {
                Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
                Frame.GoBack();
            }
        }
    }
}
