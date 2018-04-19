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
    public static class ToastHelper
    {
        public static void ShowToast(Context context, string text, ToastLength length)
        {
            Handler handler = new Handler(Looper.MainLooper);
            handler.Post(() =>
            {
                Toast.MakeText(context, text, length).Show();
            });
        }
    }
}