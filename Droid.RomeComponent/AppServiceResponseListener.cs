using System;
using Microsoft.ConnectedDevices;
using Android.Runtime;

namespace QuickShare.Droid.RomeComponent
{

    internal class AppServiceResponseListener : Java.Lang.Object, IAppServiceResponseListener
    {
        public void ResponseReceived(AppServiceClientResponse p0)
        {
            System.Diagnostics.Debug.WriteLine("ResponseListener: ResponseReceived() : " + p0.Status.ConvertToRomeAppServiceConnectionStatus() + " - " + p0.Status.ToString());
        }
    }
}