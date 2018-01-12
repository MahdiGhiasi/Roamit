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
        TextView txtVersionNumber, txtCloudClipboardModeDescription;
        TextView linkTwitter, linkGitHub, linkPrivacyPolicy;
        EditText txtDeviceName;
        Switch swCloudClipboardActivity, swCloudClipboardMode, swUiMode, swStayInBackground, swDarkTheme;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            if (new Settings(this).Theme == AppTheme.Dark)
                SetTheme(Resource.Style.MyTheme_Dark);
            else
                SetTheme(Resource.Style.MyTheme);

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Settings);

            Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Settings";

            txtVersionNumber = FindViewById<TextView>(Resource.Id.settings_txt_version);
            txtCloudClipboardModeDescription = FindViewById<TextView>(Resource.Id.settings_cloudClipboardModeDescription);
            txtDeviceName = FindViewById<EditText>(Resource.Id.settings_deviceNameText);
            swCloudClipboardActivity = FindViewById<Switch>(Resource.Id.settings_cloudClipboardActiveSwitch);
            swCloudClipboardMode = FindViewById<Switch>(Resource.Id.settings_cloudClipboardModeSwitch);
            swUiMode = FindViewById<Switch>(Resource.Id.settings_uiModeSwitch);
            swStayInBackground = FindViewById<Switch>(Resource.Id.settings_allowToStayInBackgroundSwitch);
            swDarkTheme = FindViewById<Switch>(Resource.Id.settings_darkThemeSwitch);

            txtVersionNumber.Text = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;

            linkTwitter = FindViewById<TextView>(Resource.Id.settings_twitterLink);
            linkTwitter.Click += LinkTwitter_Click;
            linkGitHub = FindViewById<TextView>(Resource.Id.settings_gitHubLink);
            linkGitHub.Click += LinkGitHub_Click;
            linkPrivacyPolicy = FindViewById<TextView>(Resource.Id.settings_privacyPolicyLink);
            linkPrivacyPolicy.Click += LinkPrivacyPolicy_Click;

            InitValues();

            Analytics.TrackPage("Settings");
        }

        private void LinkPrivacyPolicy_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("https://roamit.ghiasi.net/privacy/");
            Intent intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void LinkGitHub_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("https://www.github.com/mghiasi75/Roamit");
            Intent intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
        }

        private void LinkTwitter_Click(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("https://twitter.com/roamitapp");
            Intent intent = new Intent(Intent.ActionView, uri);
            StartActivity(intent);
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

            swDarkTheme.Checked = (settings.Theme == AppTheme.Dark);
            swDarkTheme.CheckedChange += SwDarkTheme_CheckedChange;

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

        private void SwDarkTheme_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.Theme = e.IsChecked ? AppTheme.Dark : AppTheme.Light;
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
    }
}