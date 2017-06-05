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
using Android.Support.V4.App;
using Android.Media;

namespace QuickShare.Droid.Helpers
{
    internal class ProgressNotifier
    {
        int id;
        Context context;
        NotificationManager notificationManager;
        NotificationCompat.Builder builder;

        public ProgressNotifier(Context _context)
        {
            id = Notification.GetNewNotifId();
            context = _context;

            notificationManager = NotificationManager.FromContext(_context);
        }

        public void SendInitialNotification(string title, string text)
        {
            builder = new NotificationCompat.Builder(context)
                .SetContentTitle(title)
                .SetContentText(id.ToString() + "  " + text)
                .SetSmallIcon(Resource.Drawable.Icon)
                .SetProgress(0, 0, true);

            notificationManager.Notify(id, builder.Build());
        }

        public void SetProgressValue(int max, int value)
        {
            int percent = (100 * value) / max;

            builder.SetProgress(max, value, false)
                .SetContentText(id.ToString() + "  " + $"{percent}%");

            notificationManager.Notify(id, builder.Build());
        }

        public void MakeIndetermine(string text = "")
        {
            builder.SetProgress(0, 0, true)
                .SetContentText(text);

            notificationManager.Notify(id, builder.Build());
        }

        public void FinishProgress(string title, string text)
        {
            builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                .SetPriority((int)NotificationPriority.Max)
                .SetContentTitle(title)
                .SetContentText(id.ToString() + "  " + text)
                .SetProgress(0, 0, false);

            notificationManager.Notify(id, builder.Build());
        }
    }
}