using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace QuickShare.ToastNotifications
{
    public static partial class Toaster
    {
        public static void ClearNotification(Guid guid)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) != null)
                ToastNotificationManager.History.Remove(guid.ToString());
        }

        public static void ClearNotification(string tagName)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == tagName) != null)
                ToastNotificationManager.History.Remove(tagName);
        }
    }
}
