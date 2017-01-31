using System;

namespace QuickShare.FileSendReceive
{
    public class IPDetectionCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string IP { get; set; }
    }
}