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

        public static readonly string ServerAddress = "http://localhost:14100";
    }
}
