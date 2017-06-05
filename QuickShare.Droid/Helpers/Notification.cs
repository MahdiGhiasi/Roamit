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

namespace QuickShare.Droid.Helpers
{
    internal static class Notification
    {
        private static int NotifId = 1000;

        internal static int GetNewNotifId()
        {
            NotifId++;
            return NotifId;
        }

        private static Dictionary<int, NotificationCompat.Builder> progressNotifs = new Dictionary<int, NotificationCompat.Builder>();
        private static void SendProgressNotification(Context context, int notificationKey, string title, string initialText, int percent)
        {
            NotificationCompat.Builder builder;
            var notificationManager = NotificationManager.FromContext(context);

            if (!progressNotifs.ContainsKey(notificationKey))
            {
                builder = new NotificationCompat.Builder(context)
                    .SetContentTitle(title)
                    .SetContentText(initialText)
                    .SetSmallIcon(Resource.Drawable.Icon);

                notificationManager.Notify(notificationKey, builder.Build());

                progressNotifs.Add(notificationKey, builder);
            }
            else
            {
                builder = progressNotifs[notificationKey];
            }

            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent);

            builder.SetProgress(100, percent, false);
            builder.SetContentText($"{percent}%");
            builder.SetContentIntent(pendingIntent);
            notificationManager.Notify(notificationKey, builder.Build());
        }

        private static void FinishProgressNotification(Context context, int notificationKey)
        {
            if (!progressNotifs.ContainsKey(notificationKey))
                return;

            NotificationCompat.Builder builder = progressNotifs[notificationKey];
            var notificationManager = NotificationManager.FromContext(context);

        }

        private static void SendHeadsUpNotification(Context context, string title, string body)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.OneShot);

            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var notificationBuilder =
                new NotificationCompat.Builder(context)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetPriority((int)NotificationPriority.Max)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetAutoCancel(true)
                    .SetSound(defaultSoundUri)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}