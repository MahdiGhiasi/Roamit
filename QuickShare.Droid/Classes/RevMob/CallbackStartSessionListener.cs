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
using Com.Revmob;

namespace QuickShare.Droid.Classes.RevMob
{
    class CallbackStartSessionListener : RevMobAdsListener
    {
        Activity currentActivity;
        public SessionStatus Status { get; private set; } = SessionStatus.Waiting;

        public CallbackStartSessionListener(Activity currentActivity)
        {
            Console.WriteLine("CallbackStartSessionListener");
            this.currentActivity = currentActivity;
        }

        public override void OnRevMobSessionStarted()
        {
            Console.WriteLine("Session started");
            Status = SessionStatus.Started;
            base.OnRevMobSessionStarted();
        }

        public override void OnRevMobSessionNotStarted(String error)
        {
            Console.WriteLine("RevMob session failed to start.");
            Status = SessionStatus.Failed;
        }
    }

    enum SessionStatus
    {
        Waiting, 
        Started,
        Failed,
    }
}
