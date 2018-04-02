using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    internal class FileReceiveProgressCalculator : FileTransferProgressCalculator
    {
        public FileReceiveProgressCalculator(QueueInfo queueInfo, ulong sliceMaxSize) :
            base(sliceMaxSize)
        {
            foreach (var item in queueInfo.Files)
            {
                TransferStatus.Add(item.UniqueKey, new FileTransferStatus
                {
                    LastSliceSize = item.LastSliceSize,
                    NextSlice = 0,
                    SliceMaxSize = item.SliceMaxLength,
                    SlicesCount = item.SlicesCount,
                });
            }
        }

        internal void SliceReceived(FileSendInfo file, uint receivedSlice)
        {
            SliceReceived(file.UniqueKey, receivedSlice);
        }

        internal void SliceReceived(string fileUniqueKey, uint receivedSlice)
        {
            if (!TransferStatus.ContainsKey(fileUniqueKey))
                throw new KeyNotFoundException($"FileSendInfo '{fileUniqueKey}' was not registered in FileReceiveProgressCalculator.");

            // Might go backwards (in case of a full file redownload), and that's ok.
            TransferStatus[fileUniqueKey].NextSlice = receivedSlice + 1;

            InvokeProgressEvent();
        }

    }
}
