using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Android.Media;
using Android.Support.V4.App;
using QuickShare.Droid.Services;
using QuickShare.TextTransfer;
using Android.Util;

namespace QuickShare.Droid.Firebase
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseListenerService : FirebaseMessagingService
    {
        readonly string TAG = "MyFirebaseListenerService";

        public override void OnMessageReceived(RemoteMessage message)
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
                        MessageCarrierService.ShowToast(this, "Couldn't launch URL.", Android.Widget.ToastLength.Long);
                    }
                }
                else if ((message.Data["Action"] == "FastClipboard") && (message.Data.ContainsKey("SenderName")) && (message.Data.ContainsKey("Text")))
                {
                    string senderName = message.Data["SenderName"];
                    string text = message.Data["Text"];

                    Guid guid = TextReceiver.QuickTextReceived(senderName, text);

                    MessageCarrierService.CopyTextToClipboard(this, guid);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                SendNotification("Action not supported.", "Please make sure the app is updated to enjoy latest features.");
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