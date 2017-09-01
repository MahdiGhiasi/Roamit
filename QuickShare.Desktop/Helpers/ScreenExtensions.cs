using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    // from https://stackoverflow.com/questions/29438430/how-to-get-dpi-scale-for-all-screens
    public static class ScreenExtensions
    {
        public static void GetDpi(this System.Windows.Forms.Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
        {
            var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
        }

        public static void GetScaleFactors(this System.Windows.Forms.Screen screen, out double scaleFactorX, out double scaleFactorY)
        {
            screen.GetDpi(DpiType.Effective, out uint dpix_eff, out uint dpiy_eff);
            screen.GetDpi(DpiType.Raw, out uint dpix_raw, out uint dpiy_raw);

            Debug.WriteLine($"Raw: {dpix_raw}, {dpiy_raw}");
            Debug.WriteLine($"Effectve: {dpix_eff}, {dpiy_eff}");

            scaleFactorX = ((double)dpix_raw) / ((double)dpix_eff);
            scaleFactorY = ((double)dpiy_raw) / ((double)dpiy_eff);

            if (scaleFactorX < 1)
                scaleFactorX = 1.0 / scaleFactorX;

            if (scaleFactorY < 1)
                scaleFactorY = 1.0 / scaleFactorY;

            // When raw dpi is not available (observed in a virtualbox vm), assume 1. 
            //TODO: Find an alternate way when this happens.
            if ((dpix_raw == 0) || (dpiy_raw == 0))
            {
                scaleFactorX = 1.0;
                scaleFactorY = 1.0;
            }
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In]System.Drawing.Point pt, [In]uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}
