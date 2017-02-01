using System;

namespace QuickShare.Rome
{
    public sealed class SendFinishedEventArgs
    {
        public string ErrorMessage { get; set; }
        public bool WasSendingSuccessful { get; set; }
    }
}