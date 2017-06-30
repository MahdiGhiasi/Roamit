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

namespace QuickShare.Droid
{
    [Activity]
    internal class MessageShowActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            if (Intent.GetStringExtra("message") == "trialNotice")
            {
                SetContentView(Resource.Layout.TrialNotice);                
            }
            else if (Intent.GetStringExtra("message") == "upgrade")
            {
                SetContentView(Resource.Layout.Upgrade);
            }
            else
            {
                Finish();
            }
        }
    }
}