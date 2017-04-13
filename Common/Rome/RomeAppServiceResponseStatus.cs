using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.Common.Rome
{

    //
    // Summary:
    //     Describes the status when an app tries to send a message to an app service by
    //     calling the AppServiceConnection.SendMessageAsync method.
    public enum RomeAppServiceResponseStatus
    {
        //
        // Summary:
        //     The app service successfully received and processed the message.
        Success = 0,
        //
        // Summary:
        //     The app service failed to receive and process the message.
        Failure = 1,
        //
        // Summary:
        //     The app service exited because not enough resources were available.
        ResourceLimitsExceeded = 2,
        //
        // Summary:
        //     An unknown error occurred.
        Unknown = 3,
        //
        // Summary:
        //     The device to which the message was sent is not available.
        RemoteSystemUnavailable = 4,
        //
        // Summary:
        //     The app service failed to process the message because it is too large.
        MessageSizeTooLarge = 5
    }
}
