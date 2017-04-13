
using PCLStorage;

namespace QuickShare.FileTransfer
{
    internal class FileDetails
    {
        public IFile storageFile { get; set; }
        public uint lastSliceId { get; set; }
        public uint lastSliceSize { get; set; }
        public uint lastPieceAccessed { get; set; }
    }
}