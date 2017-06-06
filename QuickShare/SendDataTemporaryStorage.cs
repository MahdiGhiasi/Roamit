using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare
{
    internal static class SendDataTemporaryStorage
    {
        public static List<IStorageItem> Files { get; set; } = new List<IStorageItem>();
        public static string Text { get; set; }
        public static Uri LaunchUri { get; set; }

        public static bool IsSharingTarget { get; set; } = false;
    }
}
