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
using Android.Gms.Analytics;

namespace QuickShare.Droid.Classes
{
    static class Analytics
    {
#if !DEBUG
        private static GoogleAnalytics GAInstance;
        private static Tracker GATracker;
#endif
        internal static void Initialize()
        {
#if !DEBUG
            GAInstance = GoogleAnalytics.GetInstance(Application.Context);
            GAInstance.SetLocalDispatchPeriod(4);
            GATracker = GAInstance.NewTracker(QuickShare.Common.Secrets.GoogleAnalyticsId);
            GATracker.SetAppName("Roamit-Android");
            GATracker.EnableExceptionReporting(true);
            GATracker.EnableAdvertisingIdCollection(true);
            GATracker.EnableAutoActivityTracking(true);
#endif
        }

        internal static void TrackPage(string pageName)
        {
#if !DEBUG
            GATracker.SetScreenName(pageName);
            GATracker.Send(new HitBuilders.ScreenViewBuilder().Build());
#endif
        }

        internal static void TrackEvent(string category, string action, string label = "")
        {
#if !DEBUG
            HitBuilders.EventBuilder builder = new HitBuilders.EventBuilder();
            builder.SetCategory(category);
            builder.SetAction(action);
            builder.SetLabel(label);

            GATracker.Send(builder.Build());
#endif
        }

        internal static void TrackException(string ExceptionMessageToTrack, bool isFatalException)
        {
#if !DEBUG
            HitBuilders.ExceptionBuilder builder = new HitBuilders.ExceptionBuilder();
            builder.SetDescription(ExceptionMessageToTrack);
            builder.SetFatal(isFatalException);

            GATracker.Send(builder.Build());
#endif
        }


    }
}