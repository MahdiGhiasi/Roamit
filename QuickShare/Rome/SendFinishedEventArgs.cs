using System;

namespace MahdiGhiasi.Rome
{
    public class SendFinishedEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public bool WasSendingSuccessful { get; set; }
    }
}