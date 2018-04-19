using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;

namespace QuickShare.Droid.Classes
{
    public static class LaunchHelper
    {
        static readonly string TAG = "LaunchHelper";

        public static void LaunchUrl(Context context, string url)
        {
            LaunchUrl(context, Android.Net.Uri.Parse(url));
        }

        public static void LaunchUrl(Context context, Android.Net.Uri url)
        {
            Intent i = new Intent(Intent.ActionView);
            i.SetData(url);
            i.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(i);
        }

        public static void OpenFile(Context context, string fileName)
        {
            OpenFile(context, Android.Net.Uri.FromFile(new Java.IO.File(fileName)), GetMimeType(fileName));
        }

        public static void OpenFile(Context context, Android.Net.Uri file, string mimeType)
        {
            try
            {
                Intent i = new Intent(Intent.ActionView);
                i.SetDataAndType(file, mimeType);
                i.AddFlags(ActivityFlags.GrantReadUriPermission);
                i.AddFlags(ActivityFlags.NewTask); // ?

                context.StartActivity(i);
            }
            catch (Exception ex)
            {
                ToastHelper.ShowToast(context, "Cannot open file.", ToastLength.Long);
                Log.Debug(TAG, "Cannot open file: " + ex.ToString());
            }
        }

        public static string GetMimeType(string file)
        {
            string type = null;
            string extension = Path.GetExtension(file).Substring(1);
            if (extension != null)
            {
                type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            }

            if (type == null)
                type = "*/*";

            return type;
        }
    }
}