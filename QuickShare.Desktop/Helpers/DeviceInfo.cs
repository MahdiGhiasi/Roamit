using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;

namespace QuickShare.Desktop.Helpers
{
    public static class DeviceInfo
    {
        public static string SystemFamily { get; }
        public static Version SystemVersion { get; }

        public static Version Rs4 { get; } = new Version("10.0.17134.0");

        static DeviceInfo()
        {
            // get the system family name
            AnalyticsVersionInfo ai = AnalyticsInfo.VersionInfo;
            SystemFamily = ai.DeviceFamily;

            // get the system version number
            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);
            SystemVersion = new Version($"{v1}.{v2}.{v3}.{v4}");
        }
    }
}
