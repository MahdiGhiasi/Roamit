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
using QuickShare.Droid.Adapters;
using QuickShare.Droid.Classes.FilePicker;
using Com.Nononsenseapps.Filepicker;

namespace QuickShare.Droid.Activities
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.settingspage")]
    internal class SettingsActivity : ThemeAwareActivity
    {
        readonly int SystemFolderPickerId = 3000;
        readonly int CustomFolderPickerId = 3001;

        TextView txtVersionNumber, txtCloudClipboardModeDescription, txtUniversalClipboardNotAvailable;
        TextView linkTwitter, linkGitHub, linkPrivacyPolicy, linkLogOut;
        EditText txtDeviceName, txtReceiveLocation;
        Switch swCloudClipboardActivity, swCloudClipboardMode, swUiMode, swDarkTheme, swUseInAppRomeProcessOnWindows, swUseSystemFolderPicker, swUseSystemFilePicker;
        Spinner groupReceivedBySpinner;

        SettingsReceivedGroupByAdapter groupReceivedByAdapter;

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
            txtUniversalClipboardNotAvailable = FindViewById<TextView>(Resource.Id.settings_universalClipboardNotAvailable);
            txtDeviceName = FindViewById<EditText>(Resource.Id.settings_deviceNameText);
            txtReceiveLocation = FindViewById<EditText>(Resource.Id.settings_fileReceiveLocationText);
            swCloudClipboardActivity = FindViewById<Switch>(Resource.Id.settings_cloudClipboardActiveSwitch);
            swCloudClipboardMode = FindViewById<Switch>(Resource.Id.settings_cloudClipboardModeSwitch);
            swUiMode = FindViewById<Switch>(Resource.Id.settings_uiModeSwitch);
            swStayInBackground = FindViewById<Switch>(Resource.Id.settings_allowToStayInBackgroundSwitch);
            swDarkTheme = FindViewById<Switch>(Resource.Id.settings_darkThemeSwitch);
            swUseInAppRomeProcessOnWindows = FindViewById<Switch>(Resource.Id.settings_showReceiveUIOnWindowsSwitch);
            swUseSystemFolderPicker = FindViewById<Switch>(Resource.Id.settings_useSystemFolderPicker);
            swUseSystemFilePicker = FindViewById<Switch>(Resource.Id.settings_useSystemFilePicker);
            groupReceivedBySpinner = FindViewById<Spinner>(Resource.Id.settings_groupReceivedBySpinner);

            txtReceiveLocation.KeyListener = null; //Disable editing of receive location text box.

            linkTwitter = FindViewById<TextView>(Resource.Id.settings_twitterLink);
            linkTwitter.Click += LinkTwitter_Click;
            linkGitHub = FindViewById<TextView>(Resource.Id.settings_gitHubLink);
            linkGitHub.Click += LinkGitHub_Click;
            linkPrivacyPolicy = FindViewById<TextView>(Resource.Id.settings_privacyPolicyLink);
            linkPrivacyPolicy.Click += LinkPrivacyPolicy_Click;
            linkLogOut = FindViewById<TextView>(Resource.Id.settings_logOutLink);
            linkLogOut.Click += LinkLogOut_Click;

            InitValues();

            Analytics.TrackPage("Settings");
        }

        private void LinkLogOut_Click(object sender, EventArgs e)
        {
            Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(this);
            builder.SetMessage("Are you sure you want to log out?")
                .SetPositiveButton("Yes", LogOutDialogClickListener)
                .SetNegativeButton("No", (IDialogInterfaceOnClickListener)null)
                .Show();
        }

        private async void LogOutDialogClickListener(object sender, DialogClickEventArgs e)
        {
            if (await ServiceFunctions.RemoveDevice(this) == false)
            {
                Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                alert.SetTitle("Failed to log out.\nPlease make sure you have a working internet connection. If the problem persists, contact us.");
                alert.SetPositiveButton("OK", (IDialogInterfaceOnClickListener)null);
                RunOnUiThread(() =>
                {
                    alert.Show();
                });

                return;
            }

            MSAAuthenticator.DeleteUserUniqueId();

            OSHelper.ClearAppDataAndExit();
            FinishAffinity();
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

            swCloudClipboardMode.Checked = (settings.CloudClipboardReceiveMode == CloudClipboardReceiveMode.Automatic);
            swCloudClipboardMode.CheckedChange += SwCloudClipboardMode_CheckedChange;

            txtUniversalClipboardNotAvailable.Visibility = CrossSecureStorage.Current.HasKey("RoamitAccountId") ? ViewStates.Gone : ViewStates.Visible;
            swCloudClipboardActivity.Visibility = CrossSecureStorage.Current.HasKey("RoamitAccountId") ? ViewStates.Visible : ViewStates.Gone;
            swCloudClipboardMode.Visibility = swCloudClipboardActivity.Visibility;
            txtCloudClipboardModeDescription.Visibility = swCloudClipboardActivity.Visibility;

            txtDeviceName.Text = settings.DeviceName;
            txtDeviceName.AfterTextChanged += TxtDeviceName_AfterTextChanged;

            swDarkTheme.Checked = (settings.Theme == AppTheme.Dark);
            swDarkTheme.CheckedChange += SwDarkTheme_CheckedChange;

            swUseInAppRomeProcessOnWindows.Checked = settings.UseInAppServiceOnWindowsDevices;
            swUseInAppRomeProcessOnWindows.CheckedChange += SwUseInAppRomeProcessOnWindows_CheckedChange;

            swUseSystemFolderPicker.Enabled = (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop);
            swUseSystemFolderPicker.Checked = settings.UseSystemFolderPicker;
            swUseSystemFolderPicker.CheckedChange += SwUseSystemFolderPicker_CheckedChange;

            swUseSystemFilePicker.Enabled = (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop);
            swUseSystemFilePicker.Checked = settings.UseSystemFilePicker;
            swUseSystemFilePicker.CheckedChange += SwUseSystemFilePicker_CheckedChange;

            txtReceiveLocation.FocusChange += TxtReceiveLocation_FocusChange;
            txtReceiveLocation.Click += TxtReceiveLocation_Click;
            txtReceiveLocation.Text = settings.DefaultDownloadFolder;

            groupReceivedByAdapter = new SettingsReceivedGroupByAdapter(this);
            groupReceivedBySpinner.Adapter = groupReceivedByAdapter;
            groupReceivedBySpinner.SetSelection(groupReceivedByAdapter.SelectedItemPosition, false);
            groupReceivedBySpinner.ItemSelected += GroupReceivedBySpinner_ItemSelected;

            txtVersionNumber.Text = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;

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

        private void SwUseSystemFolderPicker_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.UseSystemFolderPicker = e.IsChecked;
        }

        private void SwUseSystemFilePicker_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.UseSystemFilePicker = e.IsChecked;
        }

        private void GroupReceivedBySpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var item = groupReceivedByAdapter[e.Position];

            Settings settings = new Settings(this);
            settings.DownloadGroupByState = item.State;
        }

        private void SwUseInAppRomeProcessOnWindows_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.UseInAppServiceOnWindowsDevices = e.IsChecked;

            if (Common.PackageManager != null)
            {
                if (settings.UseInAppServiceOnWindowsDevices)
                    Common.PackageManager.SetAppServiceName("com.roamit.serviceinapp", "com.roamit.service");
                else
                    Common.PackageManager.SetAppServiceName("com.roamit.service");
            }
        }

        private void SwDarkTheme_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.Theme = e.IsChecked ? AppTheme.Dark : AppTheme.Light;

            Finish();
            StartActivity(Intent);
            OverridePendingTransition(Android.Resource.Animation.FadeIn, 0);
        }

        private void TxtDeviceName_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            Settings settings = new Settings(this);

            settings.DeviceName = txtDeviceName.Text;
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

        private void TxtReceiveLocation_Click(object sender, EventArgs e)
        {
            ChooseReceiveLocation();
        }

        private void TxtReceiveLocation_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
            {
                ChooseReceiveLocation();
            }
        }

        private void ChooseReceiveLocation()
        {
            Settings settings = new Settings(this);

            if (settings.UseSystemFolderPicker)
            {
                Intent i = new Intent(Intent.ActionOpenDocumentTree);
                i.AddCategory(Intent.CategoryDefault);
                i.PutExtra("android.content.extra.SHOW_ADVANCED", true);
                i.PutExtra("android.content.extra.FANCY", true);
                i.PutExtra("android.content.extra.SHOW_FILESIZE", true);
                i.PutExtra("android.provider.extra.INITIAL_URI", settings.DefaultDownloadFolder);
                StartActivityForResult(Intent.CreateChooser(i, "Choose receive location"), SystemFolderPickerId);
            }
            else
            {
                Intent i = new Intent(this, settings.Theme == AppTheme.Dark ? typeof(BackHandlingFilePickerActivityDark) : typeof(BackHandlingFilePickerActivityLight));

                i.PutExtra(FilePickerActivity.ExtraAllowCreateDir, true);
                i.PutExtra(FilePickerActivity.ExtraMode, FilePickerActivity.ModeDir);
                i.PutExtra(FilePickerActivity.ExtraStartPath, settings.DefaultDownloadFolder);

                StartActivityForResult(i, CustomFolderPickerId);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == SystemFolderPickerId)
            {
                if ((resultCode == Result.Ok) && (data != null))
                {
                    try
                    {
                        var path = FilePathHelper.GetPathForDocTree(this, data.Data);
                        ApplyReceiveLocationPathUpdate(path);
                    }
                    catch (NonPrimaryExternalStorageNotSupportedException)
                    {
                        var alert = new Android.Support.V7.App.AlertDialog.Builder(this)
                            .SetTitle("Receiving to SD Card is not currently supported.")
                            .SetMessage("This will be added in a future version.")
                            .SetPositiveButton("Ok", (s, e) => { });

                        RunOnUiThread(() =>
                        {
                            alert.Show();
                        });

                        return;
                    }
                }
            }
            else if (requestCode == CustomFolderPickerId)
            {
                if ((resultCode == Result.Ok) && (data != null))
                {
                    var path = Utils.GetSelectedFilesFromResult(data).Select(x => Utils.GetFileForUri(x).AbsolutePath).First();
                    ApplyReceiveLocationPathUpdate(path);
                }
            }
        }

        private void ApplyReceiveLocationPathUpdate(string path)
        {
            txtReceiveLocation.Text = path;

            Settings settings = new Settings(this);
            settings.DefaultDownloadFolder = path;
        }
    }
}