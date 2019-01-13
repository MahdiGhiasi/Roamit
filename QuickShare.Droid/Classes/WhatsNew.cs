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

namespace QuickShare.Droid.Classes
{
    internal class WhatsNew
    {
        Settings settings;
        Context context;

        Version appVersion;

        public WhatsNew(Context _context)
        {
            context = _context;
            settings = new Settings(_context);

            appVersion = Version.Parse(Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName);
        }

        public bool ShouldShowWhatsNew
        {
            get
            {
                var lastShown = settings.LatestShownWhatsNewVersion;

                if (lastShown >= appVersion)
                    return false;

                return IsWhatsNewItemExists(lastShown);
            }
        }

        private bool IsWhatsNewItemExists(Version lastShownVersion)
        {
            return (GetWhatsNewItems(lastShownVersion).Count > 0);
        }

        private List<string> GetWhatsNewItems(Version lastShownVersion)
        {
            var output = new List<string>();

            if (lastShownVersion < new Version("2.3.1"))
            {
                output.Add("You can now dismiss Universal Clipboard notifications.");
                output.Add("Sending large files to your PC is now more reliable.");
            }

            if (lastShownVersion < new Version("2.4.0"))
            {
                output.Add("Android to Android communication is now possible too!");
                output.Add("Dark theme");
                output.Add("Ability to change your device name");
                output.Add("Device discovery improvements");
            }

            if (lastShownVersion < new Version("2.5.0"))
            {
                output.Add("Dark theme is now available in share dialog and settings page too.");
                output.Add("Fixed an issue where back button was not working.");
            }

            if (lastShownVersion < new Version("2.6.0"))
            {
                output.Add("Improved connection speed.");
                output.Add("Improved the speed of 'Preparing' phase when sending multiple files.");
                output.Add("Fixed an issue where send link button was not working.");
            }

            if (lastShownVersion < new Version("3.0.2"))
            {
                output.Add("File transfer logic has been rewritten from scratch; It's now faster and more reliable than ever!");
                output.Add("You can now see entire receive history on the device");
                output.Add("New file picker");
                output.Add("Customize receive location");
                output.Add("Customize received files grouping");
                output.Add("Receive notification improvements");
                output.Add("Ability to move files after receive");
                output.Add("Other fixes and improvements");
            }

            if (lastShownVersion < new Version("3.2.0"))
            {
                output.Add("Windows devices can now be contacted via Roamit Cloud Service for better reliability.");
                output.Add("Sending files from SD Card is now supported.");
                output.Add("Sharing files from apps like WhatsApp to Roamit now works.");
                output.Add("Compatibility improvements for sending to Windows devices updated to October 2018 update.");
                output.Add("Other fixes and improvements");
            }

            if (lastShownVersion < new Version("3.3.0"))
            {
                output.Add("UI improvements and fixes");
                output.Add("Fixed app crash when internet connection is unreliable");
                output.Add("Fixed 'Log out' button not working");
            }


            return output;
        }

        public string GetTitle()
        {
            return $"What's new in version {appVersion.Major}.{appVersion.Minor}";
        }

        public string GetText()
        {
            var items = GetWhatsNewItems(settings.LatestShownWhatsNewVersion);
            char bulletPoint = Convert.ToChar(8226);

            StringBuilder sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.AppendLine($"{bulletPoint} {item}\n");
            }

            return sb.ToString();
        }

        public void Shown()
        {
            settings.LatestShownWhatsNewVersion = appVersion;
        }

        internal void RunMigrationCode()
        {
            var lastShownVersion = settings.LatestShownWhatsNewVersion;

            if (lastShownVersion < new Version("3.2.0"))
            {
                // Make system file picker default when updating to 3.2
                settings.UseSystemFilePicker = (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop);
            }
        }
    }
}