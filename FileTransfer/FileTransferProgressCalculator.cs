using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    internal abstract class FileTransferProgressCalculator
    {
        protected ulong sliceMaxSize;
        protected string senderName;
        protected Guid guid;

        public delegate void FileTransferProgressEventHandler(object sender, FileTransfer2ProgressEventArgs e);
        public event FileTransferProgressEventHandler FileTransferProgress;

        public Dictionary<string, FileTransferStatus> TransferStatus { get; } = new Dictionary<string, FileTransferStatus>();

        public long TotalSlices { get => TransferStatus.Select(x => x.Value.SlicesCount).Sum(x => x); }

        public FileTransferProgressCalculator(ulong sliceMaxSize) : 
            this(sliceMaxSize, "", Guid.Empty)
        {
        }

        public FileTransferProgressCalculator(ulong sliceMaxSize, string senderName, Guid guid)
        {
            this.sliceMaxSize = sliceMaxSize;
            this.senderName = senderName;
            this.guid = guid;
        }

        protected void InvokeProgressEvent()
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
                TotalFiles = TransferStatus.Count,
                SenderName = senderName,
                Guid = guid,
            });
        }
    }
}
