using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using Android.Util;
using PCLStorage;
using QuickShare.DataStore;
using QuickShare.Droid.Activities;
using Android.Support.V4.Content;

namespace QuickShare.Droid.Classes
{
    internal static class MessageReceiveHelper
    {
        static readonly string TAG = "MessageReceiveHelper";

        static ProgressNotifier progressNotifier;
        static Context context = null;

        public delegate void NotifyEventHandler();
        public static event NotifyEventHandler Activity;
        public static event NotifyEventHandler Finish;

        public static async Task<bool> ProcessReceivedMessage(Dictionary<string, object> message, Context context)
        {
            foreach (var item in message)
                Log.Debug(TAG, $"Key = {item.Key} , Value = {item.Value.ToString()}");

            if (!message.ContainsKey("Receiver"))
                return false;

            string receiver = message["Receiver"] as string;

            if (receiver == "ServerIPFinder")
            {
                if (!HasStorageWritePermission(context))
                {
                    SendPermissionErrorNotification(context);
                    return false;
                }

                InitProgressNotifier("Initializing...");

                await FileTransfer.ServerIPFinder.ReceiveRequest(message);
            }
            else if (receiver == "FileReceiver")
            {
                if (!HasStorageWritePermission(context))
                {
                    SendPermissionErrorNotification(context);
                    return false;
                }

                InitProgressNotifier("Receiving...");

                await FileTransfer.FileReceiver2.ReceiveRequest(message, new DownloadFolderDecider(context),
                    async s => { return new FileSystemFolder(s); });
            }
            else if (receiver == "TextReceiver")
            {
                await TextTransfer.TextReceiver.ReceiveRequest(message);
            }
            else if (receiver == "System")
            {
                if (message.ContainsKey("FinishService"))
                {
                    System.Diagnostics.Debug.WriteLine("Finished.");
                    return true;
                }
            }

            return false;
        }

        private static void SendPermissionErrorNotification(Context context)
        {
            Classes.Notification.SendNotification(context, "Storage permission needed", "Roamit needs Storage permission to be able to receive files.");
        }

        private static bool HasStorageWritePermission(Context context)
        {
            return (ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted);
        }

        public static void ClearEventRegistrations()
        {
            Activity = null;
            Finish = null;

            TextTransfer.TextReceiver.TextReceiveFinished -= TextReceiver_TextReceiveFinished;
            FileTransfer.FileReceiver2.FileTransferProgress -= FileReceiver_FileTransferProgress;
        }

        internal static void Init(Context _context)
        {
            DataStore.DataStorageProviders.Init(PCLStorage.FileSystem.Current.LocalStorage.Path);

            TextTransfer.TextReceiver.ClearEventRegistrations();
            TextTransfer.TextReceiver.TextReceiveFinished += TextReceiver_TextReceiveFinished;

            FileTransfer.FileReceiver2.ClearEventRegistrations();
            FileTransfer.FileReceiver2.FileTransferProgress += FileReceiver_FileTransferProgress;

            context = _context;
        }

        public static void InitProgressNotifier(string title = "Connecting...")
        {
            if (progressNotifier != null)
            {
                progressNotifier.UpdateTitle(title);
                return;
            }

            progressNotifier = new ProgressNotifier(context);
            progressNotifier.SendInitialNotification(title, "");
        }

#region Events
        private async static void TextReceiver_TextReceiveFinished(TextTransfer.TextReceiveEventArgs e)
        {
            try
            {
                Activity.Invoke();
                if (!e.Success)
                {
                    ToastHelper.ShowToast(context, "Failed to receive text.", ToastLength.Long);
                    return;
                }

                await ClipboardHelper.CopyTextToClipboard(context, (Guid)e.Guid);
                Finish?.Invoke();
            }
            finally
            {
                await progressNotifier?.ClearProgressNotification();
            }
        }

        private async static void FileReceiver_FileTransferProgress(FileTransfer.FileTransfer2ProgressEventArgs e)
        {
            Activity?.Invoke();

            if (e.State == FileTransfer.FileTransferState.Error)
            {
                await progressNotifier?.FinishProgress("Receive failed.", "");
            }
            else if (e.State == FileTransfer.FileTransferState.Finished)
            {
                var intent = new Intent(context, typeof(HistoryListActivity)); //typeof(NotificationLaunchActivity));
                intent.PutExtra("itemGuid", e.Guid.ToString());

                if (e.TotalFiles == 1)
                    await progressNotifier?.FinishProgress($"Received a file from {e.SenderName}", "Tap to view", intent, context);
                else
                    await progressNotifier?.FinishProgress($"Received {e.TotalFiles} files from {e.SenderName}", $"Tap to view", intent, context);

                progressNotifier = null;
                Finish?.Invoke();
            }
            else if (e.State == FileTransfer.FileTransferState.DataTransfer)
            {
                progressNotifier?.SetProgressValue(1000, (int)(1000.0 * e.Progress), "Receiving...");
            }
        }
#endregion

    }
}