using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QuickShare.DataStore;

namespace QuickShare.Droid.Classes
{
    internal static class ClipboardHelper
    {
        internal static string GetClipboardText(Context context)
        {
            ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);

            if ((clipboard == null) || (clipboard.Text == null))
                return "";

            return clipboard.Text;
        }

        public static async Task CopyTextToClipboard(Context context, Guid guid)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            var item = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();

            if (!(item.Data is ReceivedText))
                throw new Exception("Invalid received item type.");

            await DataStorageProviders.TextReceiveContentManager.OpenAsync();
            string text = DataStorageProviders.TextReceiveContentManager.GetItemContent(guid);
            DataStorageProviders.TextReceiveContentManager.Close();
            SetClipboardText(context, text);
        }

        public static void SetClipboardText(Context context, string text)
        {
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);
                ClipData clip = ClipData.NewPlainText(text, text);
                clipboard.PrimaryClip = clip;
            });

            ToastHelper.ShowToast(context, "Text copied to clipboard.", ToastLength.Long);
        }
    }
}