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
    }
}