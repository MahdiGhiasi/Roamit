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

namespace QuickShare.Droid.Classes
{
    public static class ShareHelper
    {
        public static void ShareText(Context context, string text)
        {
            Intent sendIntent = new Intent();
            sendIntent.SetAction(Intent.ActionSend);
            sendIntent.PutExtra(Intent.ExtraText, text);
            sendIntent.SetType("text/plain");
            context.StartActivity(Intent.CreateChooser(sendIntent, "Share"));
        }

        public static void ShareFile(Context context, Java.IO.File file)
        {
            Intent sendIntent = new Intent();
            sendIntent.SetAction(Intent.ActionSend);
            sendIntent.PutExtra(Intent.ExtraStream, Android.Net.Uri.FromFile(file));
            sendIntent.SetType(LaunchHelper.GetMimeType(file.AbsolutePath));
            context.StartActivity(Intent.CreateChooser(sendIntent, "Share"));
        }
    }
}