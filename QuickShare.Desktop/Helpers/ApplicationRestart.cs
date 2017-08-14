using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    // https://msdn.microsoft.com/en-us/library/cc303699.aspx   
    internal static class ApplicationRestart
    {
#if !SQUIRREL

        [DllImport("kernel32.dll")]
        private static extern int RegisterApplicationRestart(
            [MarshalAs(UnmanagedType.BStr)] string commandLineArgs,
            int flags);

        [Flags]
        private enum RestartRestrictions
        {
            None = 0,
            NotOnCrash = 1,
            NotOnHang = 2,
            NotOnPatch = 4,
            NotOnReboot = 8
        }

        public static void RegisterForRestart()
        {
            // Register for automatic restart if the application 
            // was terminated for any reason.
            RegisterApplicationRestart("/restart", (int)RestartRestrictions.None);
        }

#endif
    }
}
