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
using System.Threading.Tasks;

namespace QuickShare.Droid.Helpers
{
    internal class ProgressNotifier
    {
        readonly TimeSpan _minimumTimeBetweenNotifs = TimeSpan.FromSeconds(1);

        int id;
        Context context;
        NotificationManager notificationManager;
        NotificationCompat.Builder builder;

        DateTime lastProgressNotif = DateTime.MinValue;

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
                .SetContentText(text)
                .SetSmallIcon(Resource.Drawable.Icon)
                .SetProgress(0, 0, true);

            notificationManager.Notify(id, builder.Build());
            lastProgressNotif = DateTime.Now;
        }

        public void SetProgressValue(int max, int value)
        {
            if ((DateTime.Now - lastProgressNotif) < _minimumTimeBetweenNotifs)
                return;

            int percent = (100 * value) / max;

            builder.SetProgress(max, value, false)
                .SetContentText($"{percent}%");

            notificationManager.Notify(id, builder.Build());
            lastProgressNotif = DateTime.Now;
        }

        public void MakeIndetermine(string text = "")
        {
            builder.SetProgress(0, 0, true)
                .SetContentText(text);

            notificationManager.Notify(id, builder.Build());
        }

        public async void FinishProgress(string title, string text)
        {
            if ((DateTime.Now - lastProgressNotif) < _minimumTimeBetweenNotifs)
                await Task.Delay(DateTime.Now - lastProgressNotif);

            builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                .SetPriority((int)NotificationPriority.Max)
                .SetContentTitle(title)
                .SetContentText(text)
                .SetProgress(0, 0, false);

            notificationManager.Notify(id, builder.Build());
        }
    }
}