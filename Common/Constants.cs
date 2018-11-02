using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public static class Constants
    {
        public static int CommunicationPort = 8081;
        public static int IPFinderCommunicationPort = 8082;

        public static ulong FileSliceMaxLength = 512 * 1024; // 512 Kilobytes

        public static readonly string ServerAddress = "https://roamit.ghiasi.net/api"; // "http://192.168.1.100:3000";

        public static readonly string WindowsStoreAppUrl = "https://www.microsoft.com/store/apps/9nrdffns92g1";
        public static readonly string GooglePlayAppUrl = "https://play.google.com/store/apps/details?id=com.ghiasi.roamitapp";
        public static readonly string BrowserExtensionsUrl = "https://roamit.ghiasi.net/#browserExtensions";
        public static readonly string PCExtensionUrl = "https://roamit.ghiasi.net/#pcExtension";
        public static readonly string TwitterUrl = "http://twitter.com/roamitapp";
        public static readonly string GitHubUrl = "https://github.com/MahdiGhiasi/Roamit";
        public static readonly string GitHubIssuesUrl = "https://github.com/MahdiGhiasi/Roamit/issues";



        public static readonly double MaxSizeForTrialVersion = 5.0; //In Megabytes
    }
}
