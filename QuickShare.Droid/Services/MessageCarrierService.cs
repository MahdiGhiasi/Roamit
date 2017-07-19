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
using QuickShare.Droid.Classes;

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
            try
            {
                Platform.FetchAuthCode -= Platform_FetchAuthCode;
                Platform.FetchAuthCode += Platform_FetchAuthCode;
            }
            catch { }

            try
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

                Log.Debug(TAG, $"InitMessageCarrierPackageManagerIfNecessary()");
                await InitMessageCarrierPackageManagerIfNecessary();
                Log.Debug(TAG, $"InitMessageCarrierPackageManagerIfNecessary() Finished.");

                if ((intent.HasExtra("Action")) && (intent.HasExtra("DeviceId")))
                    if (intent.GetStringExtra("Action") == "SendCarrier")
                    {
                        Android.Util.Log.Debug(TAG, "2: SendCarrier");
                        SendCarrier(intent.GetStringExtra("DeviceId"));
                    }

            }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Unhandled exception occured: " + ex.ToString());
                StopSelf();
            }
        }

        private void Platform_FetchAuthCode(string s)
        {
            if (!IsApplicationInForeground())
            {
                Classes.Notification.SendLaunchNotification(this, "Roamit needs your attention", "Open the app to continue receiving data from your other devices");
            }
        }

        private async Task InitMessageCarrierPackageManagerIfNecessary()
        {
            if (Common.MessageCarrierPackageManager != null)
                return;

            Log.Debug(TAG, $"Initializing MessageCarrierPackageManager... (1)");
            Common.MessageCarrierPackageManager = new RomePackageManager(this);
            Log.Debug(TAG, $"Initializing MessageCarrierPackageManager... (2)");
            Common.MessageCarrierPackageManager.Initialize("com.roamit.messagecarrierservice");

            Log.Debug(TAG, $"Initializing MessageCarrierPackageManager... (3)");
            await Common.MessageCarrierPackageManager.InitializeDiscovery();
        }

        internal static void ShowToast(Context context, string text, ToastLength length)
        {
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                Toast.MakeText(context, text, length).Show();
            });
        }

        private async void TextReceiver_TextReceiveFinished(TextTransfer.TextReceiveEventArgs e)
        {
            try
            {
                lastActiveTime = DateTime.UtcNow;
                if (!e.Success)
                {
                    ShowToast(this, "Failed to receive text.", ToastLength.Long);
                    return;
                }

                CopyTextToClipboard(this, (Guid)e.Guid);
            }
            finally
            {
                await progressNotifier?.ClearProgressNotification();
            }
        }

        internal static async void CopyTextToClipboard(Context context, Guid guid)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            var item = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();

            if (!(item.Data is ReceivedText))
                throw new Exception("Invalid received item type.");

            await DataStorageProviders.TextReceiveContentManager.OpenAsync();
            string text = DataStorageProviders.TextReceiveContentManager.GetItemContent(guid);
            DataStorageProviders.TextReceiveContentManager.Close();

            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
                ClipData clip = ClipData.NewPlainText(text, text);
                clipboard.PrimaryClip = clip;
            });

            ShowToast(context, "Text copied to clipboard.", ToastLength.Long);
        }

        private async void FileReceiver_FileTransferProgress(FileTransfer.FileTransferProgressEventArgs e)
        {
            lastActiveTime = DateTime.UtcNow;

            if (e.State == FileTransfer.FileTransferState.Error)
            {
                await progressNotifier?.FinishProgress("Receive failed.", "");
            }
            else if (e.State == FileTransfer.FileTransferState.Finished)
            {
                if (e.TotalFiles == 1)
                {
                    var intent = new Intent(this, typeof(NotificationLaunchActivity));
                    intent.PutExtra("action", "openFile");
                    intent.PutExtra("guid", e.Guid.ToString());

                    await progressNotifier?.FinishProgress($"Received a file from {e.SenderName}", "Tap to open", intent, this);
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
            }
            else if (e.State == FileTransfer.FileTransferState.DataTransfer)
            {
                progressNotifier?.SetProgressValue((int)e.Total, (int)e.CurrentPart, "Receiving...");
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

            Android.Util.Log.Debug(TAG, "3 " + deviceId);

            InitProgressNotifier();

            //try finding remote system for 15 seconds
            for (int i = 0; i < 30; i++)
            {
                rs = Common.MessageCarrierPackageManager.RemoteSystems.FirstOrDefault(x => x.Id == deviceId);
                RemoteSystem rs2;
                try
                {
                    rs2 = Common.PackageManager.RemoteSystems.FirstOrDefault(x => x.Id == deviceId);
                }
                catch
                {

                }
                

                if (rs != null)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            Android.Util.Log.Debug(TAG, "4");

            if (rs == null)
            {
                System.Diagnostics.Debug.WriteLine($"Couldn't find device {deviceId}");
                StopSelf();
                return;
            }

            Android.Util.Log.Debug(TAG, "5");

            string thisDeviceUniqueId = ServiceFunctions.GetDeviceUniqueId();

            Android.Util.Log.Debug(TAG, "6 " + thisDeviceUniqueId);

            try
            {
                while (true)
                {
                    lastActiveTime = DateTime.Now;

                    Android.Util.Log.Debug(TAG, "Connecting to message carrier service...");
                    var c = await Common.MessageCarrierPackageManager.Connect(rs, false);
                    //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
                    //Common.MessageCarrierPackageManager.CloseAppService();
                    //c = await Common.MessageCarrierPackageManager.Connect(rs, false);

                    if (c != RomeAppServiceConnectionStatus.Success)
                    {
                        Android.Util.Log.Debug(TAG, $"Connection failed. {c.ToString()}");
                        throw new Exception($"Connection failed. {c.ToString()}");
                    }

                    Android.Util.Log.Debug(TAG, "Connected.");
                    Android.Util.Log.Debug(TAG, "Sending message carrier...");

                    var data = new Dictionary<string, object>()
                    {
                        {"SenderId", thisDeviceUniqueId },
                    };

                    var response = await Common.MessageCarrierPackageManager.Send(data);

                    Android.Util.Log.Debug(TAG, $"Response received. ({response.Status.ToString()})");

                    if ((response.Message == null) || (response.Message.Count == 0))
                    {
                        Android.Util.Log.Debug(TAG, "Response is empty.");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    var isFinished = await ProcessReceivedMessage(response.Message);

                    Android.Util.Log.Debug(TAG, "Finished.");

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
            finally
            {
                Common.MessageCarrierPackageManager.CloseAppService();
            }

            //StopSelf();
        }

        private async Task<bool> ProcessReceivedMessage(Dictionary<string, object> message)
        {
            foreach (var item in message)
                Log.Debug(TAG, $"Key = {item.Key} , Value = {item.Value.ToString()}");

            if (!message.ContainsKey("Receiver"))
                return false;

            string receiver = message["Receiver"] as string;

            if (receiver == "ServerIPFinder")
            {
                InitProgressNotifier();
                progressNotifier.UpdateTitle("Initializing...");

                await FileTransfer.ServerIPFinder.ReceiveRequest(message);
            }
            else if (receiver == "FileReceiver")
            {
                InitProgressNotifier();
                progressNotifier.UpdateTitle("Receiving...");

                string downloadPath = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "Roamit");
                System.IO.Directory.CreateDirectory(downloadPath); //Make sure download folder exists.
                IFolder downloadFolder = new FileSystemFolder(downloadPath);

                await FileTransfer.FileReceiver.ReceiveRequest(message, downloadFolder);
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

        private void InitProgressNotifier()
        {
            if (progressNotifier != null)
                return;

            progressNotifier = new ProgressNotifier(this, "Receive failed.");
            progressNotifier.SendInitialNotification("Connecting...", "");
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

        private bool IsApplicationInForeground()
        {
            ActivityManager activityManager = (ActivityManager)GetSystemService(Context.ActivityService);
            List<ActivityManager.RunningAppProcessInfo> services = activityManager.RunningAppProcesses.ToList();
            bool isActivityFound = false;

            if (services[0].ProcessName.ToLower() == PackageName.ToLower())
            {
                isActivityFound = true;
            }

            return isActivityFound;
        }
    }
}