namespace QuickShare.FileSendReceive
{
    public class FileTransferProgressEventArgs
    {
        public ulong CurrentPart { get; set; }
        public ulong Total { get; set; }
    }
}