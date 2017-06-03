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

        public static ulong FileSliceMaxLength = 512 * 1024; // 512 Kilobytes

        public static readonly string ServerAddress = "http://192.168.1.100:3000";

        public static readonly string GooglePlayAppUrl = "http://www.ghiasi.net"; //TODO: enter correct url here
    }
}
