using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace QuickShare.Common
{
    public static class ToastFunctions
    {
        public static void SendToast(string text)
        {
            const ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public static void SendToast2(string text)
        {
            const ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));

            var toast = new ScheduledToastNotification(toastXml, DateTimeOffset.Now.AddSeconds(20));
            ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
        }
    }
}
