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
    internal static class ClipboardHelper
    {
        internal static string GetClipboardText(Context context)
        {
            ClipboardManager clipboard = (ClipboardManager)context.GetSystemService(Context.ClipboardService);

            if ((clipboard == null) || (clipboard.Text == null))
                return "";

            return clipboard.Text;
        }
    }
}