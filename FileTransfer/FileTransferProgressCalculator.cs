using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransfer
{
    internal class FileTransferProgressCalculator
    {
        private ulong sliceMaxSize;

        public delegate void FileTransferProgressEventHandler(object sender, FileTransfer2ProgressEventArgs e);
        public event FileTransferProgressEventHandler FileTransferProgress;

        public Dictionary<string, FileTransferStatus> TransferStatus { get; } = new Dictionary<string, FileTransferStatus>();

        public long TotalSlices { get => TransferStatus.Select(x => x.Value.SlicesCount).Sum(x => x); }

        public FileTransferProgressCalculator(ulong sliceMaxSize)
        {
            this.sliceMaxSize = sliceMaxSize;
        }

        public void AddFileSliceSender(FileSliceSender fileSliceSender)
        {
            TransferStatus.Add(fileSliceSender.UniqueKey, new FileTransferStatus
            {
                NextSlice = 0,
                SlicesCount = fileSliceSender.SlicesCount,
                LastSliceSize = fileSliceSender.LastSliceSize,
                SliceMaxSize = sliceMaxSize,
            });
        }

        internal void SliceRequestReceived(FileSliceSender sender, SliceRequestedEventArgs e)
        {
            if (!TransferStatus.ContainsKey(sender.UniqueKey))
                throw new KeyNotFoundException($"FileSliceSender '{sender.UniqueKey}' was not registered in FileTransferProgressCalculator.");

            var nextSlice = e.RequestedSlice + 1;
            if (TransferStatus[sender.UniqueKey].NextSlice < nextSlice)
                TransferStatus[sender.UniqueKey].NextSlice = nextSlice;

            InvokeProgressEvent();
        }

        private void InvokeProgressEvent()
        {
            if (FileTransferProgress == null)
                return;

            var totalSize = TransferStatus.Select(x => x.Value.TotalSize).Sum(x => (double)x);
            var transferredSize = TransferStatus.Select(x => x.Value.TransferredSize).Sum(x => (double)x);

            FileTransferProgress?.Invoke(this, new FileTransfer2ProgressEventArgs
            {
                State = FileTransferState.DataTransfer,
                TotalBytes = totalSize,
                TotalTransferredBytes = transferredSize,
            });
        }

        internal void InitTimeout(TaskCompletionSource<FileTransferResult> transferTcs)
        {
            // TODO
        }
    }
}