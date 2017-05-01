#define NOTIFICATIONHANDLER_DEBUGINFO

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
#if NOTIFICATIONHANDLER_DEBUGINFO
            Debug.WriteLine("Notification received: " + e.CurrentPart + " / " + e.Total + " (" + e.State.ToString() + ")");
#endif
            bool UISuccess = false;
            if ((CoreApplication.MainView.CoreWindow?.Dispatcher != null) && (MainPage.Current != null))
            {
                UISuccess = true;
#if NOTIFICATIONHANDLER_DEBUGINFO
                Debug.WriteLine("Dispatcher present and MainPage exists.");
#endif
                await DispatcherEx.RunTaskAsync(CoreApplication.MainView.CoreWindow.Dispatcher, async () =>
                {
                    //If app is minimized, send notifications but update the title too.
                    if (!Window.Current.Visible)
                        UISuccess = false;

#if NOTIFICATIONHANDLER_DEBUGINFO
                    Debug.WriteLine("Window.Current.Visible is true");
#endif

                    await MainPage.Current.FileTransferProgress(e);
                });
            }

#if NOTIFICATIONHANDLER_DEBUGINFO
            Debug.WriteLine("So?");
#endif

            if ((UISuccess) && (e.State != FileTransferState.Finished))
            {
                Toaster.ClearNotification(e.Guid);
                return;
            }

#if NOTIFICATIONHANDLER_DEBUGINFO
            Debug.WriteLine("Nope");
#endif

            if (e.State == FileTransferState.Finished)
            {
                Toaster.ShowFileReceiveFinishedNotification(e.TotalFiles, e.SenderName, e.Guid);
            }
            else
            {
                double percent = ((double)e.CurrentPart) / ((double)e.Total);
                Toaster.ShowFileReceiveProgressNotification(e.SenderName, percent, e.Guid);
            }
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