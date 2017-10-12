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

namespace QuickShare.Droid
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

                OpenFile(Android.Net.Uri.FromFile(new Java.IO.File(fileName)), GetMimeType(fileName));
            }

            Finish();
        }

        private void OpenFile(Android.Net.Uri file, string mimeType)
        {
            try
            {
                Intent openFile = new Intent(Intent.ActionView);
                openFile.SetDataAndType(file, mimeType);
                openFile.AddFlags(ActivityFlags.GrantReadUriPermission);

                StartActivity(openFile);
            }
            catch (Exception ex)
            {
                MessageReceiveHelper.ShowToast(this, "Cannot open file.", ToastLength.Long);
                Log.Debug(TAG, "Cannot open file: " + ex.ToString());
            }
        }

        private string GetMimeType(string file)
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