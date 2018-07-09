using System;
using System.Collections.Generic;

namespace QuickShare.DataStore
{
    public interface IReceivedData
    {
    }

    public class ReceivedFileCollection : IReceivedData
    {
        public List<ReceivedFile> Files { get; set; } = new List<ReceivedFile>();
        public string StoreRootPath { get; set; }
    }

    public class ReceivedFile
    {
        public string StorePath { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public bool? Completed { get; set; }
        public bool? DownloadStarted { get; set; }
        public string OriginalName { get; set; }
    }

    public class ReceivedText : IReceivedData
    {

    }

    public class ReceivedUrl : IReceivedData
    {
        public Uri Uri { get; set; }
    }
}