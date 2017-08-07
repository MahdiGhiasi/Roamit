using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Preferences;
using Android.Util;

namespace QuickShare.Droid.Services
{
    [Service]
    internal class CloudClipboardService : IntentService
    {
        public CloudClipboardService() :
            base("CloudClipboardService")
        {

        }

        protected override void OnHandleIntent(Intent intent)
        {
            if (intent.Action == "CloudClipboardCopy")
            {
                var settings = new Classes.Settings(this);

                var text = settings.CloudClipboardText;

                Handler handler = new Handler(Looper.MainLooper);
                handler.Post(() =>
                {
                    try
                    {
                        ClipboardManager clipboard = (ClipboardManager)GetSystemService(Context.ClipboardService);
                        ClipData clip = ClipData.NewPlainText(text, text);
                        clipboard.PrimaryClip = clip;

                        Toast.MakeText(this, "Copied", ToastLength.Short).Show();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("CloudClipboardService", ex.Message);
                    }
                });
            }
        }
    }
}