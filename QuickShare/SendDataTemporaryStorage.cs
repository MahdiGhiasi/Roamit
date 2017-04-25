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
        public static List<StorageFile> Files { get; set; } = new List<StorageFile>();
        public static string Text { get; set; }
        public static Uri LaunchUri { get; set; }
    }
}
