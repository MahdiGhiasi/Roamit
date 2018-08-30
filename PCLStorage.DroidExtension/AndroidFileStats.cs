
using System;

namespace PCLStorage.Droid
{
    public class AndroidFileStats : IFileStats
    {
        public string Name { get; internal set; }

        public string Extension { get; internal set; }

        public DateTime CreationTime { get; internal set; }

        public DateTime CreationTimeUTC { get; internal set; }

        public DateTime LastAccessTime { get; internal set; }

        public DateTime LastAccessTimeUTC { get; internal set; }

        public DateTime LastWriteTime { get; internal set; }

        public DateTime LastWriteTimeUTC { get; internal set; }

        public long Length { get; internal set; }
    }
}