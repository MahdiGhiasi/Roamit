using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransfer
{
    internal class FileSendProgressCalculator : FileTransferProgressCalculator
    {
        public FileSendProgressCalculator(ulong sliceMaxSize) :
            base(sliceMaxSize)
        {
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
                throw new KeyNotFoundException($"FileSliceSender '{sender.UniqueKey}' was not registered in FileSendProgressCalculator.");

            var nextSlice = e.RequestedSlice + 1;
            if (TransferStatus[sender.UniqueKey].NextSlice < nextSlice)
                TransferStatus[sender.UniqueKey].NextSlice = nextSlice;

            InvokeProgressEvent();
        }

        internal void InitTimeout(TaskCompletionSource<FileTransferResult> transferTcs)
        {
            // TODO
        }
    }
}