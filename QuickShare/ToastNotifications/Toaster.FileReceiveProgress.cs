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
            System.Diagnostics.Debug.WriteLine("Notif" + percent);

            //return;

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

        /**
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
        /**/

        /**/
        private static void ShowFileReceiveProgressNotificationPreCreators(string hostName, double percent, Guid guid)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) != null)
                return;

            string toastXml = Templates.BasicText.Replace("{title}", $"Receiving from {hostName}...")
                                                 .Replace("{subtitle}", "Open the app to see transfer progress.")
                                                 .Replace("{argsLaunch}", "action=fileProgress");

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc)
            {
                SuppressPopup = true,
                Tag = guid.ToString()
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
        /**/


        private static void ShowFileReceiveProgressNotificationCreators(string hostName, double percent, Guid guid)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) == null)
            {
                string toastXml = Templates.ProgressBar.Replace("{title}", $"Receiving from {hostName}...")
                                                       .Replace("{argsLaunch}", "action=fileProgress")
                                                       .Replace("{progressTitle}", "")
                                                       .Replace("{progressValueStringOverride}", "")
                                                       .Replace("{progressStatus}", "");

                var doc = new XmlDocument();
                doc.LoadXml(toastXml);

                var toast = new ToastNotification(doc)
                {
                    SuppressPopup = true,
                    Tag = guid.ToString()
                };

                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            
            NotificationData data = new NotificationData();
            data.Values.Add("progressValue", percent.ToString());

            ToastNotificationManager.CreateToastNotifier().Update(data, guid.ToString());
        }

        private static void ShowFileReceiveProgressNotificationCreatorsForPhone(string hostName, double percent, Guid guid)
        {
            if (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == guid.ToString()) == null)
            {
                string toastXml = Templates.BasicText.Replace("{title}", $"Receiving from {hostName}...")
                                                     .Replace("{argsLaunch}", "action=fileProgress");

                var doc = new XmlDocument();
                doc.LoadXml(toastXml);

                var toast = new ToastNotification(doc)
                {
                    SuppressPopup = true,
                    Tag = guid.ToString()
                };

                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }

            int percentR = ((int)(Math.Round(100.0 * percent)));
            string percentString = (percentR < 0) ? "Initializing" : (percentR.ToString() + "%");

            NotificationData data = new NotificationData();
            data.Values.Add("subtitle", percentString);

            ToastNotificationManager.CreateToastNotifier().Update(data, guid.ToString());
        }
    }
}
