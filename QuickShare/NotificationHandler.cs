using System;
using System.Threading.Tasks;
using QuickShare.FileTransfer;
using Windows.Foundation.Collections;
using QuickShare.Common;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using System.Diagnostics;
using QuickShare.TextTransfer;
using QuickShare.DataStore;
using QuickShare.ToastNotifications;

namespace QuickShare
{
    internal static class NotificationHandler
    {
        internal static async Task HandleAsync(FileTransferProgressEventArgs e)
        {
            Debug.WriteLine("Notification received: " + e.CurrentPart + " / " + e.Total + " (" + e.State.ToString() + ")");
            bool UISuccess = false;
            if ((CoreApplication.MainView.CoreWindow?.Dispatcher != null) && (MainPage.Current != null))
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
            {
                Toaster.ClearNotification(e.Guid);
                return;
            }

            Debug.WriteLine("Nope");

            double percent = ((double)e.CurrentPart) / ((double)e.Total);
            Toaster.ShowFileReceiveProgressNotification("remote device", percent, e.Guid);
        }

        internal static async Task HandleAsync(TextReceiveEventArgs e)
        {
            ToastFunctions.SendToast("Received text with guid " + (e.Guid?.ToString() ?? "null") + " and it was " + (e.Success ? "successful" : "not successful") + ".");

            DataStorageProviders.TextReceiveContentManager.Open();

            string content = DataStorageProviders.TextReceiveContentManager.GetItemContent((Guid)e.Guid);

            DataStorageProviders.TextReceiveContentManager.Close();

            ToastFunctions.SendToast(content);
        }
    }
}