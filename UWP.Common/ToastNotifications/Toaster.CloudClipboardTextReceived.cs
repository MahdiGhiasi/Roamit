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
    public static partial class Toaster
    {
        private static readonly string CloudClipboardTag = "CLOUD_CLIPBOARD_TOAST";

        public static void ShowCloudClipboardTextReceivedNotification(string text)
        {
            if (DeviceInfo.SystemVersion >= DeviceInfo.CreatorsUpdate)
            {
                ShowCloudClipboardTextReceivedNotificationCreators(text);
            }
            else
            {
                ShowCloudClipboardTextReceivedNotificationPreCreators(text);
            }
        }

        private static void ShowCloudClipboardTextReceivedNotificationCreators(string text)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (((localSettings.Values.ContainsKey("LastToast")) && (localSettings.Values["LastToast"].ToString() != CloudClipboardTag)) || 
                (ToastNotificationManager.History.GetHistory().FirstOrDefault(x => x.Tag == CloudClipboardTag) == null))
            {
                string toastXml = Templates.BasicText.Replace("{argsLaunch}", $"action=cloudClipboard");

                var doc = new XmlDocument();
                doc.LoadXml(toastXml);

                var toast = new ToastNotification(doc)
                {
                    SuppressPopup = true,
                    NotificationMirroring = NotificationMirroring.Disabled,
                    Tag = CloudClipboardTag,
                    Data = new NotificationData(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("title", "Cloud clipboard - Tap to copy"),
                                                                                         new KeyValuePair<string, string>("subtitle", text) })
                };

                ToastNotificationManager.CreateToastNotifier().Show(toast);

                localSettings.Values["LastToast"] = CloudClipboardTag;
                return;
            }

            NotificationData data = new NotificationData();
            data.Values.Add("subtitle", text);

            ToastNotificationManager.CreateToastNotifier().Update(data, CloudClipboardTag);
        }

        private static void ShowCloudClipboardTextReceivedNotificationPreCreators(string text)
        {
            string toastXml = Templates.BasicText.Replace("{title}", "Cloud clipboard - Tap to copy")
                                                 .Replace("{subtitle}", text)
                                                 .Replace("{argsLaunch}", $"action=cloudClipboard");

            var doc = new XmlDocument();
            doc.LoadXml(toastXml);

            var toast = new ToastNotification(doc)
            {
                SuppressPopup = true,
                NotificationMirroring = NotificationMirroring.Disabled,
                Tag = CloudClipboardTag,
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
