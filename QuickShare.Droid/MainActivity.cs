using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Collections.ObjectModel;
using QuickShare.Droid.RomeComponent;
using System.Collections.Generic;
using QuickShare.DevicesListManager;
using Microsoft.ConnectedDevices;
using Android.Webkit;
using System.Linq;
using Android.Content;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using QuickShare.Droid.Services;
using QuickShare.Droid.OnlineServiceHelpers;
using Firebase.Iid;
using Firebase;
using System.Threading;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Views;
using Android.Net;
using QuickShare.Droid.Classes;
using Com.Revmob;
using Com.Revmob.Ads.Banner;
using QuickShare.Droid.Classes.RevMob;
using Android.Runtime;

namespace QuickShare.Droid
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.mainpage")]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            StartActivity(new Intent(this, typeof(WebViewContainerActivity)));
            Finish();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }
    }
}

