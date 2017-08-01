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
using QuickShare.Droid.Services;
using Android.Support.V4.App;

namespace QuickShare.Droid.Classes
{
    internal static class CloudClipboardNotifier
    {
        internal static void SendCloudClipboardNotification(Context context, string receivedText)
        {
            var intent = new Intent(context, typeof(CloudClipboardService));
            intent.SetAction("CloudClipboardCopy");

            var pendingIntent = PendingIntent.GetService(context, 0, intent, PendingIntentFlags.UpdateCurrent);

            var notificationBuilder =
                new NotificationCompat.Builder(context)
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetPriority((int)NotificationPriority.Min)
                    .SetContentTitle("Cloud clipboard - Tap to copy")
                    .SetContentText(receivedText)
                    .SetOngoing(true)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(1, notificationBuilder.Build());
        }
    }
}