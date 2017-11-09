using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Net;
using System.Net.Sockets;

namespace QuickShare.Droid.Classes
{
    internal class NetworkHelper
    {
        public static List<string> GetLocalIPAddresses()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString()).ToList();
        }
    }
}