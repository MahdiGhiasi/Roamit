using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace QuickShare.ToastNotifications
{
    internal static partial class Toaster // :D
    {
        private static Dictionary<Guid, string> fileReceiveProgresses = new Dictionary<Guid, string>();

        public static void ShowFileReceiveProgressNotification(string hostName, double percent, Guid guid)
        {
            Version creators = new Version("10.0.15063.0");

            if (DeviceInfo.SystemVersion >= creators)
            {
                if (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone)
                    ShowFileReceiveProgressNotificationCreatorsForPhone(hostName, percent, guid);
                else
                    ShowFileReceiveProgressNotificationCreators(hostName, percent, guid);
            }
            else
            {
                ShowFileReceiveProgressNotificationPreCreators(hostName, percent, guid);
            }
        }

        static DateTime lastNotifTime = DateTime.MinValue;
        private static void ShowFileReceiveProgressNotificationPreCreators(string hostName, double percent, Guid guid)
        {
            string percentString = ((int)(Math.Round(100.0 * percent))).ToString() + "%";

            if ((fileReceiveProgresses.ContainsKey(guid)) && (fileReceiveProgresses[guid] == percentString))
                return;

            if (DateTime.Now - lastNotifTime < TimeSpan.FromSeconds(4))
                return;

            lastNotifTime = DateTime.Now;

            fileReceiveProgresses[guid] = percentString;

            string toastXml = Templates.BasicText.Replace("{title}", $"Receiving from {hostName}...")
                                                 .Replace("{subtitle}", percentString);

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc);
            toast.SuppressPopup = true;
            toast.Tag = guid.ToString();

            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) != null)
                ToastNotificationManager.History.Remove(guid.ToString());

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private static void ShowFileReceiveProgressNotificationCreators(string hostName, double percent, Guid guid)
        {
            ShowFileReceiveProgressNotificationPreCreators(hostName, percent, guid);
        }

        private static void ShowFileReceiveProgressNotificationCreatorsForPhone(string hostName, double percent, Guid guid)
        {
            ShowFileReceiveProgressNotificationPreCreators(hostName, percent, guid);
        }
    }
}
