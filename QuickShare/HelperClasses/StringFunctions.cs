using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.HelperClasses
{
    public static class StringFunctions
    {
        public static string GetSpeedString(double d)
        {
            if (d < 0)
                return "";
            else if (d <= 1024)
                return $"{(int)d} bytes/s";
            else if (d <= 1024 * 1024)
                return $"{(int)(d / 1024)} KB/s";
            else
                return $"{String.Format("{0:0.0}", (d / (1024 * 1024)))} MB/s";
        }

        public static string GetSizeString(double d)
        {
            if (d < 0)
                return "";
            else if (d <= 1024)
                return $"{(int)d} bytes";
            else if (d <= 1024 * 1024)
                return $"{(int)(d / 1024)} KB";
            else if (d <= 1024 * 1024 * 1024)
                return $"{String.Format("{0:0.0}", (d / (1024 * 1024)))} MB";
            else
                return $"{String.Format("{0:0.0}", (d / (1024 * 1024 * 1024)))} GB";
        }
    }
}
