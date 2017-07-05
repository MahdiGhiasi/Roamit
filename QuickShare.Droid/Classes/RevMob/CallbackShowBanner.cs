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
    class CallbackShowBanner : RevMobAdsListener
    {
        Activity currentActivity;

        public CallbackShowBanner(Activity currentActivity)
        {
            this.currentActivity = currentActivity;
            Console.WriteLine("CallbackShowBanner");
        }

        public override void OnRevMobAdNotReceived(String error)
        {
            Console.WriteLine("Banner not received!");
        }

        public override void OnRevMobAdReceived()
        {
            Console.WriteLine("Banner ad received and ready to be displayed.");
        }

        public override void OnRevMobAdDismissed()
        {
            Console.WriteLine("Banner dismissed!");
        }

        public override void OnRevMobAdClicked()
        {
            Console.WriteLine("Banner clicked!");
        }

        public override void OnRevMobAdDisplayed()
        {
            Console.WriteLine("Banner displayed!");
        }

    }
}