using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace QuickShare
{
    public sealed partial class ShareWaiting : Page
    {
        DispatcherTimer timer;

        public ShareWaiting()
        {
            this.InitializeComponent();
        }

        private async void Timer_Tick(object sender, object e)
        {
            if (MainPage.Current == null)
            {
                timer.Stop();
                //await Task.Delay(TimeSpan.FromSeconds(1));
                //Frame.Navigate(typeof(MainPage), "BackFromShareTarget");
                App.Current.Exit();
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Frame.BackStack.Clear();

            await Task.Delay(TimeSpan.FromSeconds(5));

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
    }
}
