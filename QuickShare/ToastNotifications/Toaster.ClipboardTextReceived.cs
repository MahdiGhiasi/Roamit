using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace QuickShare.ToastNotifications
{
    internal static partial class Toaster
    {
        internal static void ShowClipboardTextReceivedNotification(Guid guid, string hostName)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) != null)
                return;

            string toastXml = Templates.BasicText.Replace("{title}", $"Received text from {hostName}")
                                                 .Replace("{subtitle}", "Tap here to copy it to clipboard.")
                                                 .Replace("{argsLaunch}", $"action=clipboardReceive&amp;guid={guid.ToString()}");

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Disabled,
                Tag = guid.ToString(),
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
