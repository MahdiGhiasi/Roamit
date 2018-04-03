using System;

namespace QuickShare.FileTransfer
{
    public class FileTransferProgressEventArgs
    {
        public ulong CurrentPart { get; set; }
        public ulong Total { get; set; }
        public FileTransferState State { get; set; }
        public string Message { get; set; } = "";
        public Guid Guid { get; set; }
        public string SenderName { get; set; }
        public int TotalFiles { get; set; }
        public ulong TotalBytesTransferred { get; set; }
    }

    public class FileTransfer2ProgressEventArgs
    {
        public FileTransferState State { get; internal set; }
        public double TotalBytes { get; internal set; }
        public double TotalTransferredBytes { get; internal set; }
        public double Progress { get => TotalTransferredBytes / TotalBytes; }
        public string SenderName { get; set; }
        public int TotalFiles { get; set; }
        public Guid Guid { get; set; }
        public Exception Exception { get; internal set; }
    }

    public enum FileTransferState
    {
        NotSet = 0,
        QueueList = 1,
        DataTransfer = 2,
        Finished = 3,
        Error = 4,
    }
}