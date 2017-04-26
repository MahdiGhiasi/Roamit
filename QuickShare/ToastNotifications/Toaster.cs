using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace QuickShare.ToastNotifications
{
    internal static partial class Toaster
    {
        internal static void ClearNotification(Guid guid)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) != null)
                ToastNotificationManager.History.Remove(guid.ToString());
        }

        internal static void ClearNotification(string tagName)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == tagName) != null)
                ToastNotificationManager.History.Remove(tagName);
        }
    }
}
