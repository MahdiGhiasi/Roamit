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
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using QuickShare.Droid.Classes;
using Plugin.SecureStorage;
using QuickShare.Droid.OnlineServiceHelpers;

namespace QuickShare.Droid
{
    [Activity]
    internal class SettingsActivity : AppCompatActivity
    {
        TextView txtVersionNumber, txtTrialStatus;
        Button btnUpgrade;
        Switch swCloudClipboardActivity, swCloudClipboardMode;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Settings";

            txtVersionNumber = FindViewById<TextView>(Resource.Id.settings_txt_version);
            txtTrialStatus = FindViewById<TextView>(Resource.Id.settings_txt_trialStatus);
            btnUpgrade = FindViewById<Button>(Resource.Id.settings_btn_upgrade);
            swCloudClipboardActivity = FindViewById<Switch>(Resource.Id.settings_cloudClipboardActiveSwitch);
            swCloudClipboardMode = FindViewById<Switch>(Resource.Id.settings_cloudClipboardModeSwitch);

            txtVersionNumber.Text = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;

            if (TrialHelper.UserTrialStatus == QuickShare.Common.Service.UpgradeDetails.VersionStatus.TrialVersion)
            {
                txtTrialStatus.Text = "Free version";
                btnUpgrade.Visibility = ViewStates.Visible;
            }
            else
            {
                txtTrialStatus.Text = "Full version";
                btnUpgrade.Visibility = ViewStates.Gone;
            }

            btnUpgrade.Click += BtnUpgrade_Click;

            InitValues();

            Analytics.TrackPage("Settings");
        }

        private async void InitValues()
        {
            Settings settings = new Settings(this);

            swCloudClipboardMode.Checked = (settings.CloudClipboardReceiveMode == CloudClipboardReceiveMode.Automatic);
            swCloudClipboardMode.CheckedChange += SwCloudClipboardMode_CheckedChange;

            swCloudClipboardActivity.Visibility = CrossSecureStorage.Current.HasKey("RoamitAccountId") ? ViewStates.Visible : ViewStates.Gone;

            if (CrossSecureStorage.Current.HasKey("RoamitAccountId"))
            {
                swCloudClipboardMode.Enabled = false;

                var cloudClipboardActivated = await ServiceFunctions.GetCloudClipboardActivationStatus();
                swCloudClipboardActivity.Checked = cloudClipboardActivated;

                if (cloudClipboardActivated)
                    swCloudClipboardMode.Enabled = true;

                swCloudClipboardActivity.CheckedChange += SwCloudClipboardActivity_CheckedChange;
            }
        }

        private void SwCloudClipboardMode_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            if (e.IsChecked)
                settings.CloudClipboardReceiveMode = CloudClipboardReceiveMode.Automatic;
            else
                settings.CloudClipboardReceiveMode = CloudClipboardReceiveMode.Notification;
        }

        private async void SwCloudClipboardActivity_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            swCloudClipboardMode.Enabled = e.IsChecked;

            await ServiceFunctions.SetCloudClipboardActivationStatus(e.IsChecked);
        }

        private void BtnUpgrade_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MessageShowActivity));
            intent.PutExtra("message", "upgrade");
            StartActivity(intent);
        }
    }
}