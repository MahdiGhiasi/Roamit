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
    }
}