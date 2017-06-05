using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Util;
using System.Threading;
using Microsoft.ConnectedDevices;
using QuickShare.Common.Rome;
using System.Threading.Tasks;
using PCLStorage;
using QuickShare.DataStore;
using QuickShare.Droid.RomeComponent;
using QuickShare.Droid.OnlineServiceHelpers;
using QuickShare.Droid.Helpers;

namespace QuickShare.Droid.Services
{
    [Service]
    public class MessageCarrierService : Service
    {
        readonly TimeSpan _maxIdleLifeSpan = TimeSpan.FromMinutes(2);

        static readonly string TAG = "X:" + typeof(MessageCarrierService).Name;
        static readonly int TimerWait = 4000;
        Timer timer;
        DateTime startTime;
        DateTime lastActiveTime;
        bool isStarted = false;

        ProgressNotifier progressNotifier;

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            InitService(intent, flags, startId);
            return StartCommandResult.Sticky;
        }

        private async void InitService(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, $"OnStartCommand called at {startTime}, flags={flags}, startid={startId}");
            if (isStarted)
            {
                TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
                Log.Debug(TAG, $"This service was already started, it's been running for {runtime:c}.");
            }
            else
            {
                isStarted = true;
                startTime = DateTime.UtcNow;
                Log.Debug(TAG, $"Starting the service, at {startTime}.");
                timer = new Timer(HandleTimerCallback, startTime, 0, TimerWait);

                DataStore.DataStorageProviders.Init(PCLStorage.FileSystem.Current.LocalStorage.Path);

                TextTransfer.TextReceiver.ClearEventRegistrations();
                TextTransfer.TextReceiver.TextReceiveFinished += TextReceiver_TextReceiveFinished;

                FileTransfer.FileReceiver.ClearEventRegistrations();
                FileTransfer.FileReceiver.FileTransferProgress += FileReceiver_FileTransferProgress;
            }

            lastActiveTime = DateTime.UtcNow;

            await InitMessageCarrierPackageManagerIfNecessary();

            if (intent.GetStringExtra("Action") == "SendCarrier")
            {
                SendCarrier(intent.GetStringExtra("DeviceId"));
            }
        }

        private async Task InitMessageCarrierPackageManagerIfNecessary()
        {
            if (Common.MessageCarrierPackageManager != null)
                return;

            Common.MessageCarrierPackageManager = new RomePackageManager(this);
            Common.MessageCarrierPackageManager.Initialize("com.quickshare.messagecarrierservice");

            await Common.MessageCarrierPackageManager.InitializeDiscovery();
        }

        private void ShowToast(string text, ToastLength length)
        {
            var context = this;
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                Toast.MakeText(context, text, length).Show();
            });
        }

        private void TextReceiver_TextReceiveFinished(TextTransfer.TextReceiveEventArgs e)
        {
            lastActiveTime = DateTime.UtcNow;
            if (!e.Success)
            {
                ShowToast("Failed to receive text.", ToastLength.Long);
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

            ShowToast("Text copied to clipboard.", ToastLength.Long);
        }

        private void FileReceiver_FileTransferProgress(FileTransfer.FileTransferProgressEventArgs e)
        {
            lastActiveTime = DateTime.UtcNow;

            if (e.State == FileTransfer.FileTransferState.Error)
            {
                progressNotifier?.FinishProgress("Receive failed.", "");
            }
            else if (e.State == FileTransfer.FileTransferState.Finished)
            {
                progressNotifier?.FinishProgress("Receive completed.", "Tap to view");
            }
            else if (e.State == FileTransfer.FileTransferState.DataTransfer)
            {
                progressNotifier?.SetProgressValue((int)e.Total, (int)e.CurrentPart);
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            // This is a started service, not a bound service, so we just return null.
            return null;
        }

        private async void SendCarrier(string deviceId)
        {
            RemoteSystem rs = null;

            //try finding remote system for 15 seconds
            for (int i = 0; i < 30; i++)
            {
                rs = Common.MessageCarrierPackageManager.RemoteSystems.FirstOrDefault(x => x.Id == deviceId);

                if (rs != null)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            if (rs == null)
            {
                System.Diagnostics.Debug.WriteLine($"Couldn't find device {deviceId}");
                StopSelf();
                return;
            }

            string thisDeviceUniqueId = ServiceFunctions.GetDeviceUniqueId();

            try
            {
                while (true)
                {
                    lastActiveTime = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine("Connecting to message carrier service...");
                    var c = await Common.MessageCarrierPackageManager.Connect(rs, false);
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
                        {"SenderId", thisDeviceUniqueId },
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
                progressNotifier = new ProgressNotifier(this);
                progressNotifier.SendInitialNotification("Receiving...", "Connecting to remote device...");

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
            TextTransfer.TextReceiver.TextReceiveFinished -= TextReceiver_TextReceiveFinished;
            FileTransfer.FileReceiver.FileTransferProgress -= FileReceiver_FileTransferProgress;

            TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"Service destroyed at {DateTime.UtcNow} after running for {runtime:c}.");
            base.OnDestroy();
        }

        void HandleTimerCallback(object state)
        {
            TimeSpan runTime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"This service has been running for {runTime:c} (since ${state}).");

            TimeSpan timeElapsedSinceLastActivity = DateTime.UtcNow.Subtract(lastActiveTime);
            if (timeElapsedSinceLastActivity > _maxIdleLifeSpan)
            {
                Log.Debug(TAG, $"Service is idle for {timeElapsedSinceLastActivity:c}, will shut down.");
                StopSelf();
            }
        }
    }
}