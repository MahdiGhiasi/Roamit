using Windows.Storage;

namespace QuickShare.FileSendReceive
{
    internal class FileDetails
    {
        public StorageFile storageFile { get; set; }
        public uint lastSliceId { get; set; }
        public uint lastSliceSize { get; set; }
        public uint lastPieceAccessed { get; set; }
    }
}