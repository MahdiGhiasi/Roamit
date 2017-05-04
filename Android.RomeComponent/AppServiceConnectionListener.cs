using System;
using Microsoft.ConnectedDevices;

namespace QuickShare.Droid.RomeComponent
{
    internal class AppServiceConnectionListener : Java.Lang.Object, IAppServiceClientConnectionListener
    {
        public void OnClosed(AppServiceClientClosedStatus p0)
        {
            System.Diagnostics.Debug.WriteLine("ConnectionListener: OnClosed()");
        }

        public void OnError(AppServiceClientConnectionStatus p0)
        {
            System.Diagnostics.Debug.WriteLine("ConnectionListener: OnError()");
        }

        public void OnSuccess()
        {
            System.Diagnostics.Debug.WriteLine("ConnectionListener: OnSuccess()");
        }
    }
}