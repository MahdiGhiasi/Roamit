using QuickShare.Common;
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
using QuickShare.FileTransfer;

namespace QuickShare
{
    public sealed partial class MainReceive : Page
    {
        public MainReceiveViewModel ViewModel { get; set; }

        bool shouldStayOpen = false;

        public MainReceive()
        {
            this.InitializeComponent();

            ViewModel = new MainReceiveViewModel
            {
                DontCloseWindowNoticeVisibility = (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone) ? Visibility.Collapsed : Visibility.Visible,
                DontSwitchAppsNoticeVisibility = (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone) ? Visibility.Visible : Visibility.Collapsed,
                ProgressIsIndeterminate = true,
                ReceiveStatus = "Preparing to receive...",
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            shouldStayOpen = (bool)e.Parameter;

            base.OnNavigatedTo(e);
        }

        private void Finish()
        {
            if (shouldStayOpen)
            {
                //Find parent frame
                var p = Frame.Parent as FrameworkElement;
                while (!(p is Frame))
                    p = p.Parent as FrameworkElement;

                (p as Frame).GoBack();
            }
            else
            {
                Application.Current.Exit();
            }
        }

        internal void FileTransferProgress(FileTransferProgressEventArgs e)
        {
            if (e.State == FileTransferState.Finished)
            {
                Finish();
                return;
            }

            if (e.Total == 0)
                return;

            ViewModel.ProgressIsIndeterminate = false;
            ViewModel.ProgressPercentIndicatorVisibility = Visibility.Visible;
            ViewModel.ReceiveStatus = "Receiving...";

            ViewModel.ProgressValue = (int)e.CurrentPart;
            ViewModel.ProgressMaximum = (int)e.Total;
        }
    }
}
