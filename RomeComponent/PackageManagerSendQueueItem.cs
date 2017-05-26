using QuickShare.Common.Rome;
using System;
using System.Collections.Generic;

namespace QuickShare.UWP.Rome
{
    public class PackageManagerSendQueueItem
    {
        public string RemoteSystemId { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public delegate void SendEventHandler(PackageManagerSendQueueItemEventArgs e);
        public event SendEventHandler SendFinished;

        public void SetSendResult(RomeAppServiceResponseStatus status)
        {
            SendFinished?.Invoke(new PackageManagerSendQueueItemEventArgs
            {
                ResponseStatus = status,
            });
        }
    }

    public class PackageManagerSendQueueItemEventArgs
    {
        public RomeAppServiceResponseStatus ResponseStatus { get; set; }
    }
}