using System;
using System.Threading.Tasks;
using QuickShare.FileSendReceive;
using Windows.Foundation.Collections;
using QuickShare.Common;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using System.Diagnostics;

namespace QuickShare
{
    internal static class NotificationHandler
    {
        internal static async Task HandleAsync(FileTransferProgressEventArgs e)
        {
            Debug.WriteLine("Notification received: " + e.CurrentPart + " / " + e.Total + " (" + e.State.ToString() + ")");
            bool UISuccess = false;
            if ((CoreApplication.MainView.CoreWindow.Dispatcher != null) && (MainPage.Current != null))
            {
                UISuccess = true;
                Debug.WriteLine("Dispatcher present and MainPage exists.");
                await DispatcherEx.RunTaskAsync(CoreApplication.MainView.CoreWindow.Dispatcher, async () =>
                {
                    //If app is minimized, send notifications but update the title too.
                    if (!Window.Current.Visible)
                        UISuccess = false;

                    Debug.WriteLine("Window.Current.Visible is true");

                    await MainPage.Current.FileTransferProgress(e);
                });
            }

            Debug.WriteLine("So?");

            if (UISuccess)
                return;

            Debug.WriteLine("Nope");

            ToastFunctions.SendToast(e.CurrentPart + " / " + e.Total);
        }
    }
}