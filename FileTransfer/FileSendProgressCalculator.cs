using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    internal class FileSendProgressCalculator : FileTransferProgressCalculator
    {
        readonly TimeSpan maximumInactiveTime = TimeSpan.FromSeconds(6);
        readonly int maxTimeoutCount = 2;

        Timer timer;
        int timeoutCountSinceLastSlice = 0;
        private TaskCompletionSource<FileTransferResult> timeoutTcs;

        internal bool TransferStarted { get; private set; } = false;

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

            TransferStarted = true;
            timeoutCountSinceLastSlice = 0;
            UpdateTimer();
            InvokeProgressEvent();
        }

        private void UpdateTimer()
        {
            timer.Change((int)maximumInactiveTime.TotalMilliseconds, Timeout.Infinite);
        }

        internal void InitTimeout(TaskCompletionSource<FileTransferResult> timeoutTcs)
        {
            timer = new Timer(OnTimeout, null, (int)maximumInactiveTime.TotalMilliseconds, Timeout.Infinite);
            this.timeoutTcs = timeoutTcs;
        }

        private void OnTimeout(object state)
        {
            timeoutCountSinceLastSlice++;
            if (timeoutCountSinceLastSlice > maxTimeoutCount)
                timeoutTcs.TrySetResult(FileTransferResult.FailedOnSend);
            else
                timeoutTcs.TrySetResult(FileTransferResult.Timeout);
        }
    }
}