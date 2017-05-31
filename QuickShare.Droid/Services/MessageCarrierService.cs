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
using Android.Util;
using System.Threading;
using Microsoft.ConnectedDevices;
using QuickShare.Common.Rome;
using System.Threading.Tasks;
using PCLStorage;
using QuickShare.DataStore;

namespace QuickShare.Droid.Services
{
    [Service]
    public class MessageCarrierService : Service
    {
        static readonly string TAG = "X:" + typeof(MessageCarrierService).Name;
        static readonly int TimerWait = 4000;
        Timer timer;
        DateTime startTime;
        bool isStarted = false;

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, $"OnStartCommand called at {startTime}, flags={flags}, startid={startId}");
            if (isStarted)
            {
                TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
                Log.Debug(TAG, $"This service was already started, it's been running for {runtime:c}.");

                SendCarrier();
            }
            else
            {
                startTime = DateTime.UtcNow;
                Log.Debug(TAG, $"Starting the service, at {startTime}.");

                DataStore.DataStorageProviders.Init(PCLStorage.FileSystem.Current.LocalStorage.Path);
                TextTransfer.TextReceiver.TextReceiveFinished += TextReceiver_TextReceiveFinished;
                SendCarrier();

                timer = new Timer(HandleTimerCallback, startTime, 0, TimerWait);
                isStarted = true;
            }
            return StartCommandResult.Sticky;
        }

        private void TextReceiver_TextReceiveFinished(TextTransfer.TextReceiveEventArgs e)
        {
            if (!e.Success)
            {
                Toast.MakeText(this, "Failed to receive text.", ToastLength.Long).Show();
                return;
            }

            DataStorageProviders.HistoryManager.Open();
            var item = DataStorageProviders.HistoryManager.GetItem((Guid)e.Guid);
            DataStorageProviders.HistoryManager.Close();

            if (!(item.Data is ReceivedText))
                throw new Exception("Invalid received item type.");

            DataStorageProviders.TextReceiveContentManager.Open();
            string text = DataStorageProviders.TextReceiveContentManager.GetItemContent((Guid)e.Guid);
            DataStorageProviders.TextReceiveContentManager.Close();

            ClipboardManager clipboard = (ClipboardManager)GetSystemService(Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText(text, text);
            clipboard.PrimaryClip = clip;

            Toast.MakeText(this, "Copied text to clipboard.", ToastLength.Long).Show();
        }

        public override IBinder OnBind(Intent intent)
        {
            // This is a started service, not a bound service, so we just return null.
            return null;
        }

        private async void SendCarrier()
        {
            var files = (await PCLStorage.FileSystem.Current.LocalStorage.GetFilesAsync()).ToList();
            try
            {
                while (true)
                {
                    System.Diagnostics.Debug.WriteLine("Connecting to message carrier service...");
                    var c = await Common.MessageCarrierPackageManager.Connect(Common.GetCurrentRemoteSystemForMessageCarrier(), false);
                    //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
                    //Common.PackageManager.CloseAppService();
                    //c = await Common.PackageManager.Connect(rs, false);

                    if (c != RomeAppServiceConnectionStatus.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Connection failed. {c.ToString()}");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine("Connected.");
                    System.Diagnostics.Debug.WriteLine("Sending message carrier...");

                    var data = new Dictionary<string, object>()
                {
                    {"SenderId", ":)" },
                };

                    var response = await Common.MessageCarrierPackageManager.Send(data);

                    System.Diagnostics.Debug.WriteLine($"Response received. ({response.Status.ToString()})");

                    if (response.Message == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Response is empty.");
                        continue;
                    }

                    var isFinished = await ProcessReceivedMessage(response.Message);

                    System.Diagnostics.Debug.WriteLine("Finished.");

                    if (isFinished)
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Exception in SendCarrier()");
                Log.Debug(TAG, ex.Message);
                Log.Debug(TAG, ex.ToString());
            }


            StopSelf();
        }

        private async Task<bool> ProcessReceivedMessage(Dictionary<string, object> message)
        {
            foreach (var item in message)
                System.Diagnostics.Debug.WriteLine($"Key = {item.Key} , Value = {item.Value.ToString()}");

            string receiver = message["Receiver"] as string;

            if (receiver == "ServerIPFinder")
            {
                await FileTransfer.ServerIPFinder.ReceiveRequest(message);
            }
            else if (receiver == "FileReceiver")
            {
                string downloadPath = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "QuickShare");
                System.IO.Directory.CreateDirectory(downloadPath); //Make sure download folder exists.
                IFolder downloadFolder = new FileSystemFolder(downloadPath);

                await FileTransfer.FileReceiver.ReceiveRequest(message, downloadFolder);
            }
            else if (receiver == "TextReceiver")
            {
                TextTransfer.TextReceiver.ReceiveRequest(message);
            }
            else if (receiver == "System")
            {
                if (message.ContainsKey("FinishService"))
                {
                     System.Diagnostics.Debug.WriteLine("Goodbye");
                     return true;
                }
            }

            return false;
        }

        public override void OnDestroy()
        {
            timer.Dispose();
            timer = null;
            isStarted = false;

            TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"Simple Service destroyed at {DateTime.UtcNow} after running for {runtime:c}.");
            base.OnDestroy();
        }

        void HandleTimerCallback(object state)
        {
            TimeSpan runTime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"This service has been running for {runTime:c} (since ${state}).");
        }
    }
}