using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace QuickShare.ToastNotifications
{
    public static partial class Toaster
    {
        public static void ShowFileReceiveFailedNotification(Guid guid, Exception exception)
        {
            //TODO: Better fail reason message based on received FileTransferFailReason.

            ClearNotification(guid); //Clear progress notification

            string toastXml = Templates.BasicText.Replace("{title}", "Receive failed.")
                                                 .Replace("{subtitle}", "Make sure devices are on the same network and the app is remained open on the sender device.")
                                                 .Replace("{argsLaunch}", "");

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc)
            {
                NotificationMirroring = NotificationMirroring.Disabled,
                Tag = guid.ToString(),
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["LastToast"] = guid.ToString();
        }
    }
}
