namespace QuickShare.FileTransfer
{
    internal class FileTransferStatus
    {
        public uint NextSlice { get; set; }
        public uint SlicesCount { get; set; }
        public ulong LastSliceSize { get; set; }
        public ulong SliceMaxSize { get; set; }

        public ulong TotalSize
        {
            get
            {
                if (SlicesCount == 0)
                    return 0;
                return ((SlicesCount - 1) * SliceMaxSize + LastSliceSize);
            }
        }

        public ulong TransferredSize
        {
            get
            {
                if (SlicesCount == 0)
                    return 0;
                if (NextSlice == SlicesCount)
                    return (SlicesCount - 1) * SliceMaxSize + LastSliceSize;
                return NextSlice * SliceMaxSize;
            }
        }
    }
}