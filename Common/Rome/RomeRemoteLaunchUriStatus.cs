using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.Common.Rome
{

    //
    // Summary:
    //     Specifies the result of activating an application for a URI on a remote device.
    public enum RomeRemoteLaunchUriStatus
    {
        //
        // Summary:
        //     The URI could not be successfully launched on the remote system.
        Unknown = 0,
        //
        // Summary:
        //     The URI was successfully launched on the remote system.
        Success = 1,
        //
        // Summary:
        //     The app is not installed on the remote system.
        AppUnavailable = 2,
        //
        // Summary:
        //     The application you are trying to activate on the remote system does not support
        //     this URI.
        ProtocolUnavailable = 3,
        //
        // Summary:
        //     The remote system could not be reached.
        RemoteSystemUnavailable = 4,
        //
        // Summary:
        //     The amount of data you tried to send to the remote system exceeded the limit.
        ValueSetTooLarge = 5,
        //
        // Summary:
        //     The user is not authorized to launch an app on the remote system.
        DeniedByLocalSystem = 6,
        //
        // Summary:
        //     The user is not signed in on the target device or may be blocked by group policy.
        DeniedByRemoteSystem = 7
    }

}
