namespace FileTransfer
{
    internal class FileTransferStatus
    {
        public uint NextSlice { get; set; }
        public uint SlicesCount { get; set; }
        public ulong LastSliceSize { get; set; }
        public ulong SliceMaxSize { get; set; }

        public ulong TotalSize { get => (SlicesCount - 1) * SliceMaxSize + LastSliceSize; }
        public ulong TransferredSize
        {
            get
            {
                if (NextSlice == SlicesCount)
                    return (SlicesCount - 1) * SliceMaxSize + LastSliceSize;
                else
                    return NextSlice * SliceMaxSize;
            }
        }
    }
}