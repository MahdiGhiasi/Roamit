using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Android.Media;
using Android.Support.V4.App;
using QuickShare.Droid.Services;

namespace QuickShare.Droid.Firebase
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseListenerService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            if (!message.Data.ContainsKey("Action"))
            {
                SendNotification("Invalid data.", "");
            }
            else if (message.Data["Action"] == "Wake")
            {
                SendNotification("Wake", "Wake");

                var intent = new Intent(this, typeof(MessageCarrierService));
                intent.PutExtra("Action", "Wake");
                StartService(intent);
            }
            else if (message.Data["Action"] == "SendCarrier")
            {
                SendNotification("SendCarrier", "SendCarrier");

                var intent = new Intent(this, typeof(MessageCarrierService));
                intent.PutExtra("Action", "SendCarrier");
                intent.PutExtra("DeviceId", message.Data["WakerDeviceId"]);

                Android.Util.Log.Debug("CARRIER_DEBUG", "1");

                StartService(intent);
            }
            else
            {
                SendNotification("Invalid action", message.Data["Action"]);
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