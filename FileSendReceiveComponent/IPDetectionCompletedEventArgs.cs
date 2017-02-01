using System;

namespace QuickShare.FileSendReceive
{
    public class IPDetectionCompletedEventArgs
    {
        public bool Success { get; set; }
        public string IP { get; set; }
    }
}