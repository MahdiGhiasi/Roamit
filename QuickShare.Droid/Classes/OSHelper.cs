using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Lang;

namespace QuickShare.Droid.Classes
{
    public static class OSHelper
    {
        public static void ClearAppDataAndExit()
        {
            //ScheduleAppRestart(context);
            ((ActivityManager)Application.Context.GetSystemService(Context.ActivityService)).ClearApplicationUserData();
            //JavaSystem.Exit(2);
        }

        //private static void ScheduleAppRestart(Context context)
        //{
        //    Intent intent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
        //    intent.PutExtra("logOut", true);
        //    intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.ClearTask | ActivityFlags.NewTask);
        //    PendingIntent pendingIntent = PendingIntent.GetActivity(MainApplication.GetInstance().BaseContext, 0, intent, PendingIntentFlags.OneShot);
        //    AlarmManager mgr = (AlarmManager)MainApplication.GetInstance().BaseContext.GetSystemService(Context.AlarmService);
        //    mgr.Set(AlarmType.Rtc, JavaSystem.CurrentTimeMillis() + 1000, pendingIntent);
        //}

        public static void ClearWebViewCache(Context context)
        {
            var mWebView = new WebView(context);

            mWebView.ClearCache(true);
            mWebView.ClearHistory();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.LollipopMr1)
            {
                CookieManager.Instance.RemoveAllCookies(null);
                CookieManager.Instance.Flush();
            }
            else
            {
                CookieSyncManager cookieSyncMngr = CookieSyncManager.CreateInstance(context);
                cookieSyncMngr.StartSync();
                CookieManager cookieManager = CookieManager.Instance;
                cookieManager.RemoveAllCookie();
                cookieManager.RemoveSessionCookie();
                cookieSyncMngr.StopSync();
                cookieSyncMngr.Sync();
            }
        }

    }
}