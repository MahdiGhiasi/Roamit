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
using QuickShare.Common.Rome;
using Microsoft.ConnectedDevices;
using QuickShare.DevicesListManager;

namespace QuickShare.Droid.RomeComponent
{
    public static class Extensions
    {
        public static RomeRemoteLaunchUriStatus ConvertToRomeRemoteLaunchUriStatus(this RemoteLaunchUriStatus launchUriStatus)
        {
            if (launchUriStatus.Value == RemoteLaunchUriStatus.AppUnavailable.Value)
                return RomeRemoteLaunchUriStatus.AppUnavailable;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.DeniedByLocalSystem.Value)
                return RomeRemoteLaunchUriStatus.DeniedByLocalSystem;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.DeniedByRemoteSystem.Value)
                return RomeRemoteLaunchUriStatus.DeniedByRemoteSystem;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.ProtocolUnavailable.Value)
                return RomeRemoteLaunchUriStatus.ProtocolUnavailable;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.RemoteSystemUnavailable.Value)
                return RomeRemoteLaunchUriStatus.RemoteSystemUnavailable;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.Success.Value)
                return RomeRemoteLaunchUriStatus.Success;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.Unknown.Value)
                return RomeRemoteLaunchUriStatus.Unknown;
            else if (launchUriStatus.Value == RemoteLaunchUriStatus.BundleTooLarge.Value)
                return RomeRemoteLaunchUriStatus.ValueSetTooLarge;
            else
                return RomeRemoteLaunchUriStatus.Unknown;
        }

        public static RomeAppServiceResponseStatus ConvertToRomeAppServiceConnectionStatus(this AppServiceResponseStatus status)
        {
            if (status.Value == AppServiceResponseStatus.Failure.Value)
                return RomeAppServiceResponseStatus.Failure;
            else if (status.Value == AppServiceResponseStatus.MessageSizeTooLarge.Value)
                return RomeAppServiceResponseStatus.MessageSizeTooLarge;
            else if (status.Value == AppServiceResponseStatus.ResourceLimitsExceeded.Value)
                return RomeAppServiceResponseStatus.ResourceLimitsExceeded;
            else if (status.Value == AppServiceResponseStatus.Success.Value)
                return RomeAppServiceResponseStatus.Success;
            else if (status.Value == AppServiceResponseStatus.RemoteSystemUnavailable.Value)
                return RomeAppServiceResponseStatus.RemoteSystemUnavailable;
            else if (status.Value == AppServiceResponseStatus.Unknown.Value)
                return RomeAppServiceResponseStatus.Unknown;
            else 
                return RomeAppServiceResponseStatus.Unknown;
        }

        public static NormalizedRemoteSystemStatus ConvertToNormalizedRemoteSystemStatus(this RemoteSystemStatus status)
        {
            if (status.Value == RemoteSystemStatus.Available.Value)
                return NormalizedRemoteSystemStatus.Available;
            else if (status.Value == RemoteSystemStatus.DiscoveringAvailability.Value)
                return NormalizedRemoteSystemStatus.DiscoveringAvailability;
            else if (status.Value == RemoteSystemStatus.Unavailable.Value)
                return NormalizedRemoteSystemStatus.Unavailable;
            else if (status.Value == RemoteSystemStatus.Unknown.Value)
                return NormalizedRemoteSystemStatus.Unknown;
            else
                return NormalizedRemoteSystemStatus.Unknown;
        }

        public static RomeAppServiceConnectionStatus ConvertToRomeAppServiceConnectionStatus(this AppServiceConnectionStatus status)
        {
            if (status.Value == AppServiceConnectionStatus .AppNotInstalled.Value)
                return RomeAppServiceConnectionStatus.AppNotInstalled;
            else if (status.Value == AppServiceConnectionStatus .AppserviceUnavailable.Value)
                return RomeAppServiceConnectionStatus.AppServiceUnavailable;
            else if (status.Value == AppServiceConnectionStatus .AppUnavailable.Value)
                return RomeAppServiceConnectionStatus.AppUnavailable;
            else if (status.Value == AppServiceConnectionStatus .NotAuthorized.Value)
                return RomeAppServiceConnectionStatus.NotAuthorized;
            else if (status.Value == AppServiceConnectionStatus .RemoteSystemNotSupportedbyapp.Value)
                return RomeAppServiceConnectionStatus.RemoteSystemNotSupportedByApp;
            else if (status.Value == AppServiceConnectionStatus .RemoteSystemUnavailable.Value)
                return RomeAppServiceConnectionStatus.RemoteSystemUnavailable;
            else if (status.Value == AppServiceConnectionStatus .Success.Value)
                return RomeAppServiceConnectionStatus.Success;
            else if (status.Value == AppServiceConnectionStatus .Unknown.Value)
                return RomeAppServiceConnectionStatus.Unknown;
            else
                return RomeAppServiceConnectionStatus.Unknown;
        }

        public static Bundle ConvertToBundle(this Dictionary<string, object> data)
        {
            var bundle = new Bundle();
            foreach (var item in data)
            {
                if (item.Value.GetType() == typeof(int))
                    bundle.PutInt(item.Key, (int)item.Value);
                else if (item.Value.GetType() == typeof(long))
                    bundle.PutLong(item.Key, (long)item.Value);
                else if (item.Value.GetType() == typeof(string))
                    bundle.PutString(item.Key, (string)item.Value);
                else
                    throw new NotSupportedException($"Adding data {item.Value.ToString()} with type {item.Value.GetType().Name} to Bundle is not supported.");
            }

            return bundle;
        }

        public static RomeAppServiceResponse ConvertToRomeAppServiceResponse(this AppServiceResponse response)
        {
            var output = new RomeAppServiceResponse();

            if (response.Status.Value == AppServiceResponseStatus.Failure.Value)
                output.Status = RomeAppServiceResponseStatus.Failure;
            else if (response.Status.Value == AppServiceResponseStatus.MessageSizeTooLarge.Value)
                output.Status = RomeAppServiceResponseStatus.MessageSizeTooLarge;
            else if (response.Status.Value == AppServiceResponseStatus.RemoteSystemUnavailable.Value)
                output.Status = RomeAppServiceResponseStatus.RemoteSystemUnavailable;
            else if (response.Status.Value == AppServiceResponseStatus.ResourceLimitsExceeded.Value)
                output.Status = RomeAppServiceResponseStatus.ResourceLimitsExceeded;
            else if (response.Status.Value == AppServiceResponseStatus.Success.Value)
                output.Status = RomeAppServiceResponseStatus.Success;
            else if (response.Status.Value == AppServiceResponseStatus.Unknown.Value)
                output.Status = RomeAppServiceResponseStatus.Unknown;
            else 
                output.Status = RomeAppServiceResponseStatus.Unknown;

            output.Message = new Dictionary<string, object>();
            foreach (var key in response.Message.KeySet())
            {
                var item = response.Message.Get(key);
                if (item == null)
                    continue;
                
                if (item.Class.Name == "java.lang.Integer")
                    output.Message.Add(key, (int)item);
                else if (item.Class.Name == "java.lang.String")
                    output.Message.Add(key, (string)item);
                else if (item.Class.Name == "java.lang.Long")
                    output.Message.Add(key, (long)item);
                else if (item.Class.Name == "java.lang.Float")
                    output.Message.Add(key, (float)item);
                else if (item.Class.Name == "java.lang.Double")
                    output.Message.Add(key, (double)item);
                else
                    throw new NotSupportedException($"Reading data type {item.Class.Name} from Bundle is not supported.");
                
            }

            return output;
        }
    }
}