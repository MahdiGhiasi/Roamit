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
        public static void ShowFileReceiveFinishedNotification(int filesCount, string hostName, Guid guid)
        {
            ClearNotification(guid); //Clear progress notification

            string toastXml = Templates.FileReceived.Replace("{title}", (filesCount == 1) ? "1 file received" : $"{filesCount} files received")
                                                    .Replace("{subtitle}", $"from {hostName}")
                                                    .Replace("{guid}", guid.ToString());

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Disabled,
                Tag = guid.ToString()
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
