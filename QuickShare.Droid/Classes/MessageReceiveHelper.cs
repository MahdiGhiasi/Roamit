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

        public static async Task<bool> ProcessReceivedMessage(Dictionary<string, object> message)
        {
            foreach (var item in message)
                Log.Debug(TAG, $"Key = {item.Key} , Value = {item.Value.ToString()}");

            if (!message.ContainsKey("Receiver"))
                return false;

            string receiver = message["Receiver"] as string;

            if (receiver == "ServerIPFinder")
            {
                InitProgressNotifier("Initializing...");

                await FileTransfer.ServerIPFinder.ReceiveRequest(message);
            }
            else if (receiver == "FileReceiver")
            {
                InitProgressNotifier("Receiving...");

                await FileTransfer.FileReceiver2.ReceiveRequest(message, new DownloadFolderDecider(context));
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

            progressNotifier = new ProgressNotifier(context, "Receive failed. (1)");
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
                if (e.TotalFiles == 1)
                {
                    var intent = new Intent(context, typeof(NotificationLaunchActivity));
                    intent.PutExtra("action", "openFile");
                    intent.PutExtra("guid", e.Guid.ToString());

                    await progressNotifier?.FinishProgress($"Received a file from {e.SenderName}", "Tap to open", intent, context);
                }
                else
                {
                    await DataStorageProviders.HistoryManager.OpenAsync();
                    var hr = DataStorageProviders.HistoryManager.GetItem(e.Guid);
                    DataStorageProviders.HistoryManager.Close();
                    var rootPath = (hr.Data as ReceivedFileCollection).StoreRootPath;
                    await progressNotifier?.FinishProgress($"Received {e.TotalFiles} files from {e.SenderName}", $"They're located at {rootPath}");
                }

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