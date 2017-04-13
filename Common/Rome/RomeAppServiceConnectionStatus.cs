using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.Common.Rome
{
    //
    // Summary:
    //     Describes the status for the attempt that an app makes to open a connection to
    //     an app service by calling the AppServiceConnection.OpenAsync method.
    public enum RomeAppServiceConnectionStatus
    {
        //
        // Summary:
        //     The connection to the app service was opened successfully.
        Success = 0,
        //
        // Summary:
        //     The package for the app service to which a connection was attempted is not installed
        //     on the device. Check that the package is installed before trying to open a connection
        //     to the app service.
        AppNotInstalled = 1,
        //
        // Summary:
        //     The package for the app service to which a connection was attempted is temporarily
        //     unavailable. Try to connect again later.
        AppUnavailable = 2,
        //
        // Summary:
        //     The app with the specified package family name is installed and available, but
        //     the app does not declare support for the specified app service. Check that the
        //     name of the app service and the version of the app are correct.
        AppServiceUnavailable = 3,
        //
        // Summary:
        //     An unknown error occurred.
        Unknown = 4,
        //
        // Summary:
        //     The device to which a connection was attempted is not available.
        RemoteSystemUnavailable = 5,
        //
        // Summary:
        //     The app does not support remote connections to the device you attempted to connect
        //     with.
        RemoteSystemNotSupportedByApp = 6,
        //
        // Summary:
        //     The user for your app is not authorized to connect to the service.
        NotAuthorized = 7
    }

}
