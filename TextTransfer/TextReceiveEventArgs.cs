using System;

namespace QuickShare.TextTransfer
{
    public class TextReceiveEventArgs
    {
        public bool Success { get; set; }
        public Guid? Guid { get; set; }
    }
}