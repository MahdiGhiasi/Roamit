using QuickShare.Classes;
using QuickShare.ToastNotifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public static class CloudClipboardHandler
    {
        public static void ReceiveRequest(Dictionary<string, object> data)
        {
            string text = data["Data"].ToString();

            //We use LocalSettings here instead of database, to have less overhead and less power consumption.
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["CloudClipboardText"] = text;
            Debug.WriteLine($"Received CloudClipboard text {text}");
            Toaster.ShowCloudClipboardTextReceivedNotification(text);
            Debug.WriteLine("Updated notification.");

            if (data.ContainsKey("AccountId"))
                SecureKeyStorage.SetAccountId(data["AccountId"].ToString());

            if (data.ContainsKey("GraphDeviceId"))
                SecureKeyStorage.SetGraphDeviceId(data["GraphDeviceId"].ToString());
        }
    }
}
