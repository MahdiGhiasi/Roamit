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

        public static readonly string ServerAddress = "https://qs.services.ghiasi.net";

        public static readonly string GooglePlayAppUrl = "http://www.ghiasi.net"; //TODO: enter correct url here

        public static readonly double MaxSizeForTrialVersion = 5.0; //In Megabytes
    }
}
