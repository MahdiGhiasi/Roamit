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

namespace QuickShare.Droid
{
    [Activity]
    internal class SettingsActivity : AppCompatActivity
    {
        TextView txtVersionNumber, txtTrialStatus;
        Button btnUpgrade;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "About";

            txtVersionNumber = FindViewById<TextView>(Resource.Id.settings_txt_version);
            txtTrialStatus = FindViewById<TextView>(Resource.Id.settings_txt_trialStatus);
            btnUpgrade = FindViewById<Button>(Resource.Id.settings_btn_upgrade);

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
        }

        private void BtnUpgrade_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(MessageShowActivity));
            intent.PutExtra("message", "upgrade");
            StartActivity(intent);
        }
    }
}