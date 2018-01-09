using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Android.Media;
using Android.Support.V4.App;
using QuickShare.Droid.Services;
using QuickShare.TextTransfer;
using Android.Util;
using QuickShare.Droid.Classes;
using Android.Preferences;
using Plugin.SecureStorage;

namespace QuickShare.Droid.Firebase
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseListenerService : FirebaseMessagingService
    {
        readonly string TAG = "MyFirebaseListenerService";

        public MyFirebaseListenerService()
        {
            SecureStorageImplementation.StoragePassword = Config.Secrets.SecureStoragePassword;
        }

        public override async void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            try
            {
                if (!message.Data.ContainsKey("Action"))
                {
                    throw new InvalidOperationException();
                }
                else if (message.Data["Action"] == "Wake")
                {
#if DEBUG
                    SendNotification("Wake", "Wake");
#endif
                    var intent = new Intent(this, typeof(MessageCarrierService));
                    intent.PutExtra("Action", "Wake");
                    StartService(intent);
                }
                else if (message.Data["Action"] == "SendCarrier")
                {
#if DEBUG
                    SendNotification("SendCarrier", "SendCarrier");
#endif
                    var intent = new Intent(this, typeof(MessageCarrierService));
                    intent.PutExtra("Action", "SendCarrier");
                    intent.PutExtra("DeviceId", message.Data["WakerDeviceId"]);

                    Android.Util.Log.Debug("CARRIER_DEBUG", "1");

                    StartService(intent);
                }
                else if (message.Data["Action"] == "Payload")
                {
#if DEBUG
                    SendNotification("Payload", "Payload");
#endif
                    var intent = new Intent(this, typeof(WaiterService));
                    intent.PutExtra("Data", message.Data["Data"]);

                    StartService(intent);
                }
                else if ((message.Data["Action"] == "LaunchUrl") && (message.Data.ContainsKey("Url")))
                {
                    try
                    {
                        string url = message.Data["Url"];

                        Intent i = new Intent(Intent.ActionView);
                        i.SetData(Android.Net.Uri.Parse(url));
                        i.SetFlags(ActivityFlags.NewTask);
                        StartActivity(i);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(TAG, ex.Message);
                        MessageReceiveHelper.ShowToast(this, "Couldn't launch URL.", Android.Widget.ToastLength.Long);
                    }
                }
                else if ((message.Data["Action"] == "FastClipboard") && (message.Data.ContainsKey("SenderName")) && (message.Data.ContainsKey("Text")))
                {
                    string senderName = message.Data["SenderName"];
                    string text = message.Data["Text"];

                    Guid guid = await TextReceiver.QuickTextReceivedAsync(senderName, text);

                    MessageReceiveHelper.CopyTextToClipboard(this, guid);
                }
                else if (message.Data["Action"] == "CloudClipboard")
                {
                    if (message.Data.ContainsKey("Data"))
                    {
                        string text = message.Data["Data"];

                        var settings = new Settings(this);

                        if (message.Data.ContainsKey("AccountId"))
                        {
                            if (CrossSecureStorage.Current.HasKey("RoamitAccountId"))
                                CrossSecureStorage.Current.DeleteKey("RoamitAccountId");

                            CrossSecureStorage.Current.SetValue("RoamitAccountId", message.Data["AccountId"]);
                        }

                        if (settings.CloudClipboardReceiveMode == CloudClipboardReceiveMode.Automatic)
                        {
                            CloudClipboardNotifier.SetCloudClipboardValue(this, text);
                        }
                        else
                        {
                            settings.CloudClipboardText = text;
                            CloudClipboardNotifier.SendCloudClipboardNotification(this, text);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                SendNotification($"Action '{message.Data["Action"]}' not supported.", "Please make sure the app is updated to enjoy latest features.");
            }
        }

        private void SendNotification(string title, string body)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            // intent.PutExtra("launchArguments", "stuff");

            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var notificationBuilder =
                new NotificationCompat.Builder(this)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetAutoCancel(true)
                    .SetSound(defaultSoundUri)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}