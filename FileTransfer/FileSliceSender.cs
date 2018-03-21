using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PCLStorage;
using QuickShare.Common;

namespace FileTransfer
{
    internal class FileSliceSender
    {
        private IFile File { get; }
        public string UniqueKey { get; }
        public uint SlicesCount { get; }
        public uint LastSliceId { get => SlicesCount - 1; }
        public ulong LastSliceSize { get; }

        public delegate void SliceRequestedEventHandler(FileSliceSender sender, SliceRequestedEventArgs e);
        public event SliceRequestedEventHandler SliceRequested;

        public FileSliceSender(FileSendInfo fileInfo)
        {
            File = fileInfo.File;
            UniqueKey = fileInfo.UniqueKey;
            SlicesCount = fileInfo.SlicesCount;
            LastSliceSize = fileInfo.LastSliceSize;
        }

        internal async Task<byte[]> GetFileSlice(IWebServer webServer, RequestDetails request)
        {
            try
            {
                string[] parts = request.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts[0];
                var id = uint.Parse(parts[1]);

                var sliceSize = GetSliceSize(id);

                SliceRequested?.Invoke(this, new SliceRequestedEventArgs
                {
                    RequestedSlice = id,
                });

                byte[] buffer = new byte[sliceSize];
                using (Stream stream = await File.OpenAsync(PCLStorage.FileAccess.Read))
                {
                    stream.Seek((int)(id * Constants.FileSliceMaxLength), SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, (int)sliceSize);
                }

                return buffer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in GetFileSlice(): " + ex.Message);
                return $"Invalid Request - {ex.Message}".Select(c => (byte)c).ToArray();
            }
        }

        private ulong GetSliceSize(ulong id)
        {
            return (LastSliceId != id) ? Constants.FileSliceMaxLength : LastSliceSize;
        }
    }
}