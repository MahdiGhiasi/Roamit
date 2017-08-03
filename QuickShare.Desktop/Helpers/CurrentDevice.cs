using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    internal class CurrentDevice
    {
        internal static string GetDeviceName()
        {
            return System.Net.Dns.GetHostName(); //Environment.MachineName;
        }
    }
}
