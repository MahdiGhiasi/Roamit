namespace QuickShare.FileTransfer
{
    public class FileTransferProgressEventArgs
    {
        public ulong CurrentPart { get; set; }
        public ulong Total { get; set; }
        public FileTransferState State { get; set; }
        public string Message { get; set; } = "";
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