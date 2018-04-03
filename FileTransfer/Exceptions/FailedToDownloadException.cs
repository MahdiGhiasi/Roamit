using System;

namespace QuickShare.FileTransfer.Exceptions
{
    public class FailedToDownloadException : Exception
    {
        public FailedToDownloadException() { }

        public FailedToDownloadException(string url, double timeout, int maxTryCount, Exception innerException) : 
            base($"Can't download '{url}', after {maxTryCount} tries and {timeout}s timeout.", innerException)
        {
        }

        public FailedToDownloadException(string url, double timeout, int maxTryCount) :
            this(url, timeout, maxTryCount, null)
        {
        }

        public FailedToDownloadException(string url, string reasonPhrase, Exception innerException) : 
            base($"Failed to download '{url}', reason was {reasonPhrase}", innerException)
        {
        }

        public FailedToDownloadException(string url, string reasonPhrase) :
            this(url, reasonPhrase, null)
        {
        }
    }
}