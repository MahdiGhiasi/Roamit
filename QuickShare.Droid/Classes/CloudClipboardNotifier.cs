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
using QuickShare.Droid.Firebase;
using Android.Util;

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
                    .SetContentTitle("Universal clipboard - Tap to copy")
                    .SetContentText(receivedText)
                    .SetOngoing(true)
                    .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(context);
            notificationManager.Notify(1, notificationBuilder.Build());
        }

        internal static void SetCloudClipboardValue(Context context, string text)
        {
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                try
                {
                    ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
                    ClipData clip = ClipData.NewPlainText(text, text);
                    clipboard.PrimaryClip = clip;

                    Toast.MakeText(context, "Clipboard updated", ToastLength.Short).Show();
                }
                catch (Exception ex)
                {
                    Log.Debug("CloudClipboardNotifier:SetCloudClipboardValue", ex.Message);
                }
            });
        }
    }
}