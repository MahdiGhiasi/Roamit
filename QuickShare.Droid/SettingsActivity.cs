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
using System.Threading.Tasks;

namespace QuickShare.Droid
{
    [Activity]
    internal class SettingsActivity : AppCompatActivity
    {
        TextView txtVersionNumber, txtTrialStatus, txtCloudClipboardModeDescription;
        EditText txtDeviceName;
        Button btnUpgrade;
        Switch swCloudClipboardActivity, swCloudClipboardMode, swUiMode, swStayInBackground;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);

            Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Settings";

            txtVersionNumber = FindViewById<TextView>(Resource.Id.settings_txt_version);
            txtTrialStatus = FindViewById<TextView>(Resource.Id.settings_txt_trialStatus);
            txtCloudClipboardModeDescription = FindViewById<TextView>(Resource.Id.settings_cloudClipboardModeDescription);
            txtDeviceName = FindViewById<EditText>(Resource.Id.settings_deviceNameText);
            btnUpgrade = FindViewById<Button>(Resource.Id.settings_btn_upgrade);
            swCloudClipboardActivity = FindViewById<Switch>(Resource.Id.settings_cloudClipboardActiveSwitch);
            swCloudClipboardMode = FindViewById<Switch>(Resource.Id.settings_cloudClipboardModeSwitch);
            swUiMode = FindViewById<Switch>(Resource.Id.settings_uiModeSwitch);
            swStayInBackground = FindViewById<Switch>(Resource.Id.settings_allowToStayInBackgroundSwitch);

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

        public override void OnBackPressed()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ServiceFunctions.RegisterDevice(this);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            base.OnBackPressed();
        }

        private async void InitValues()
        {
            Settings settings = new Settings(this);

            swUiMode.Checked = settings.UseLegacyUI;
            swUiMode.CheckedChange += SwUiMode_CheckedChange;

            swStayInBackground.Checked = settings.AllowToStayInBackground;
            swStayInBackground.CheckedChange += SwStayInBackground_CheckedChange;

            swCloudClipboardMode.Checked = (settings.CloudClipboardReceiveMode == CloudClipboardReceiveMode.Automatic);
            swCloudClipboardMode.CheckedChange += SwCloudClipboardMode_CheckedChange;

            swCloudClipboardActivity.Visibility = CrossSecureStorage.Current.HasKey("RoamitAccountId") ? ViewStates.Visible : ViewStates.Gone;
            swCloudClipboardMode.Visibility = swCloudClipboardActivity.Visibility;
            txtCloudClipboardModeDescription.Visibility = swCloudClipboardActivity.Visibility;

            txtDeviceName.Text = settings.DeviceName;
            txtDeviceName.AfterTextChanged += TxtDeviceName_AfterTextChanged;

            if (CrossSecureStorage.Current.HasKey("RoamitAccountId"))
            {
                swCloudClipboardMode.Enabled = false;
                swCloudClipboardActivity.Enabled = false;

                var cloudClipboardActivated = await ServiceFunctions.GetCloudClipboardActivationStatus();
                swCloudClipboardActivity.Checked = cloudClipboardActivated;

                swCloudClipboardActivity.Enabled = true;
                if (cloudClipboardActivated)
                    swCloudClipboardMode.Enabled = true;

                swCloudClipboardActivity.CheckedChange += SwCloudClipboardActivity_CheckedChange;
            }
        }

        private void TxtDeviceName_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.DeviceName = txtDeviceName.Text;
        }

        private void SwStayInBackground_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            if (e.IsChecked)
            {
                StartService(new Intent(this, typeof(Services.RomeReadyService)));
                settings.AllowToStayInBackground = true;
            }
            else
            {
                settings.AllowToStayInBackground = false;
                StopService(new Intent(this, typeof(Services.RomeReadyService)));
            }
        }

        private void SwUiMode_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.UseLegacyUI = e.IsChecked;
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