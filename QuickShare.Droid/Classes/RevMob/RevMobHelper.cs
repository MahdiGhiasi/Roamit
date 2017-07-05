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
using System.Threading.Tasks;

namespace QuickShare.Droid.Classes.RevMob
{
    static class RevMobHelper
    {
        internal static async Task<Com.Revmob.RevMob> TryGetAdMobSessionAsync(CallbackStartSessionListener startSessionListener)
        {
            int tryCount = 0;
            while ((Com.Revmob.RevMob.Session() == null) || (startSessionListener.Status != SessionStatus.Started))
            {
                tryCount++;
                if (tryCount > 20)
                    return Com.Revmob.RevMob.Session();
                await Task.Delay(1000);
            }
            return Com.Revmob.RevMob.Session();
        }
    }
}