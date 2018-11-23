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
using Android.Media;
using Android.Support.V4.App;
using QuickShare.Droid.Activities;

namespace QuickShare.Droid.Classes
{
    internal static class Notification
    {
        internal const string DefaultChannel = "default";
        internal const string ProgressChannel = "progress";
        internal const string UniversalClipboardChannel = "clipboard";

        internal static void SendLaunchNotification(Context context, string title, string body, string channel = DefaultChannel)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            // intent.PutExtra("launchArguments", "stuff");

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var notificationBuilder =
                new NotificationCompat.Builder(context, channel)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetStyle(new NotificationCompat.BigTextStyle()
                        .BigText(body))
                    .SetAutoCancel(true)
                    .SetSound(defaultSoundUri)
                    .SetPriority((int)NotificationPriority.Max)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(0, notificationBuilder.Build());
        }

        public static void SendDebugNotification(Context context, string title, string body, string channel = DefaultChannel)
        {
#if DEBUG
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var notificationBuilder =
                new NotificationCompat.Builder(context, channel)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetStyle(new NotificationCompat.BigTextStyle()
                        .BigText(body))
                    .SetAutoCancel(true)
                    .SetSound(defaultSoundUri)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(0, notificationBuilder.Build());
#endif
        }

        public static void SendNotification(Context context, string title, string body, string channel = DefaultChannel)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            // intent.PutExtra("launchArguments", "stuff");

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var notificationBuilder =
                new NotificationCompat.Builder(context, channel)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetStyle(new NotificationCompat.BigTextStyle()
                        .BigText(body))
                    .SetAutoCancel(true)
                    .SetSound(defaultSoundUri)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}