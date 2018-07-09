using System;

namespace QuickShare.FileTransfer
{
    internal class ReceiveCancelledException : Exception
    {
        public ReceiveCancelledException()
        {
        }

        public ReceiveCancelledException(string message) : base(message)
        {
        }

        public ReceiveCancelledException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}