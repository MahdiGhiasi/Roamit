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
using QuickShare.DataStore;
using System.IO;
using Android.Util;
using Android.Webkit;
using QuickShare.Droid.Services;
using QuickShare.Droid.Classes;

namespace QuickShare.Droid.Activities
{
    [Activity]
    internal class NotificationLaunchActivity : Activity
    {
        readonly string TAG = "NotificationLaunchActivity";

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Intent.GetStringExtra("action") == "openFile")
            {
                var guid = Guid.Parse(Intent.GetStringExtra("guid"));

                await DataStorageProviders.HistoryManager.OpenAsync();
                var hr = DataStorageProviders.HistoryManager.GetItem(guid);
                DataStorageProviders.HistoryManager.Close();

                string fileName = Path.Combine((hr.Data as ReceivedFileCollection).Files[0].StorePath, (hr.Data as ReceivedFileCollection).Files[0].Name);
                LaunchHelper.OpenFile(this, fileName);
            }

            Finish();
        }
    }
}