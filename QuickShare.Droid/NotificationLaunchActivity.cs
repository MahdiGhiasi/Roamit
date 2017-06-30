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

namespace QuickShare.Droid
{
    [Activity]
    internal class NotificationLaunchActivity : Activity
    {
        readonly string TAG = "NotificationLaunchActivity";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Intent.GetStringExtra("action") == "openFile")
            {
                var guid = Guid.Parse(Intent.GetStringExtra("guid"));

                DataStorageProviders.HistoryManager.Open();
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

                if (mimeType == "*/*")
                {
                    StartActivity(openFile);
                }
                else
                {
                    //Hide Roamit from the open with list
                    var activities = PackageManager.QueryIntentActivities(openFile, 0);
                    string packageNameToHide = "com.ghiasi.roamit";
                    var targetIntents = new List<Intent>();
                    foreach (var currentInfo in activities)
                    {
                        string packageName = currentInfo.ActivityInfo.PackageName;
                        if (packageName.ToLower() != packageNameToHide)
                        {
                            Intent targetIntent = new Intent(Android.Content.Intent.ActionView);
                            targetIntent.SetDataAndType(file, mimeType);
                            targetIntent.SetPackage(packageName);
                            targetIntents.Add(targetIntent);
                        }
                    }

                    if (targetIntents.Count > 0)
                    {
                        var intent0 = targetIntents[0];
                        targetIntents.RemoveAt(0);

                        Intent chooserIntent = Intent.CreateChooser(intent0, "Open file with");
                        chooserIntent.PutExtra(Intent.ExtraInitialIntents, targetIntents.Select(x => (IParcelable)x).ToArray());
                        StartActivity(chooserIntent);
                    }
                    else
                    {
                        MessageCarrierService.ShowToast(this, "No app found to open this type of file.", ToastLength.Long);
                    }
                }
            }
            catch (ActivityNotFoundException e)
            {
                MessageCarrierService.ShowToast(this, "Cannot open file.", ToastLength.Long);
                Log.Debug(TAG, "Cannot open file.");
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