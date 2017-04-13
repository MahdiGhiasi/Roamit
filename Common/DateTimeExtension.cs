using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public static class DateTimeExtension
    {
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTimeMilliseconds(this DateTime x)
        {
            var unixDateTime = (x.ToUniversalTime() - epoch).TotalMilliseconds;
            return (long)unixDateTime;
        }

        public static DateTime FromUnixTimeMilliseconds(long unixDateTime)
        {
            var timeSpan = TimeSpan.FromMilliseconds(unixDateTime);
            var localDateTime = epoch.Add(timeSpan).ToLocalTime();

            return localDateTime;
        }


    }
}
