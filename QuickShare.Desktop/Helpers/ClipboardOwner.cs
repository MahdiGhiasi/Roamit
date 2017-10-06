using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    static class ClipboardOwner
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardOwner();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static Process FindOwner()
        {
            try
            {
                IntPtr ownerHwnd = GetClipboardOwner();
                GetWindowThreadProcessId(ownerHwnd, out uint processId);
                Process proc = Process.GetProcessById((int)processId);

                Debug.WriteLine($"Clipboard owner is {proc.ProcessName} which is located at {proc.MainModule.FileName}");

                //TODO: Fix this for Windows 10 apps.

                return proc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Couldn't find clipboard owner: {ex.Message}");
                return null;
            }
        }
    }
}
