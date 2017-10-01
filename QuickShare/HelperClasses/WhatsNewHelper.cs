using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    static class WhatsNewHelper
    {
        public static bool ShouldShowWhatsNew()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("LatestWhatsNewVersion"))
                return true;

            if (System.Version.TryParse(ApplicationData.Current.LocalSettings.Values["LatestWhatsNewVersion"].ToString(), out System.Version v))
            {
                if (v < DeviceInfo.ApplicationVersion)
                {
                    return true;
                }
            }

            return false;
        }

        public static void InitIntro()
        {
            MarkThisWhatsNewAsRead();
        }

        public static List<string> GetWhatsNewContentId()
        {
            List<string> output = new List<string>();

            if (!ShouldShowWhatsNew())
                return output;

            System.Version prevVersion = new System.Version(0, 0, 0, 0);

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("LatestWhatsNewVersion"))
                System.Version.TryParse(ApplicationData.Current.LocalSettings.Values["LatestWhatsNewVersion"].ToString(), out prevVersion);

            if (prevVersion < new System.Version("2.0.0.0"))
                output.Add("4");

            if (prevVersion < new System.Version("1.6.1.0"))
                output.Add("3");

            //if (prevVersion < new System.Version("1.5.2.0"))
            //    output.Add("2");

            if ((prevVersion < new System.Version("1.2.1.0")) &&
                ((DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop) || (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Tablet)))
                output.Add("1");


            //Important message regarding Android app's new listing
            if ((prevVersion < new System.Version("2.1.5.0")) && 
                (SecureKeyStorage.IsUserIdStored())) 
            {
                output.Clear();
                output.Add("5");
            }

            MarkThisWhatsNewAsRead();

            return output;
        }

        private static void MarkThisWhatsNewAsRead()
        {
            ApplicationData.Current.LocalSettings.Values["LatestWhatsNewVersion"] = DeviceInfo.ApplicationVersionString;
        }
    }
}
