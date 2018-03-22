using Newtonsoft.Json;
using PCLStorage;
using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransfer
{
    internal class FileInfoListGenerator
    {
        private IEnumerable<FileSendInfo> files;
        private string ip;

        public FileInfoListGenerator(IEnumerable<FileSendInfo> files, string ip)
        {
            this.files = files;
            this.ip = ip;
        }

        public async Task<FileInfoList> GenerateAsync()
        {
            List<string> itemsLegacy = new List<string>();
            List<FileSendInfo> items = new List<FileSendInfo>();
            foreach (var item in files)
            {
                var key = item.UniqueKey;
                var stats = await item.File.GetFileStats();
                itemsLegacy.Add(GetQueueFileInfoLegacy(key, item.SlicesCount, item.FileName, stats, item.RelativePath));
                items.Add(item);
            }

            var infoList = new QueueInfo
            {
                Files = items,
            };

            return new FileInfoList
            {
                FileInfoListJsonLegacy = JsonConvert.SerializeObject(itemsLegacy),
                FileInfoListJson = JsonConvert.SerializeObject(infoList),
            };
        }

        private string GetQueueFileInfoLegacy(string key, uint slicesCount, string fileName, IFileStats properties, string directory)
        {
            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "FileReceiver" },
                { "DownloadKey", key },
                { "SlicesCount", (int)slicesCount },
                { "FileName", fileName },
                { "DateModified", properties.LastWriteTime.ToUnixTimeMilliseconds() },
                { "DateCreated", properties.CreationTime.ToUnixTimeMilliseconds() },
                { "FileSize", properties.Length },
                { "Directory", directory },
                { "ServerIP", ip },
            };

            var serialized = JsonConvert.SerializeObject(vs, Formatting.None);
            return serialized;
        }

    }

    internal class FileInfoList
    {
        public string FileInfoListJson { get; set; }
        public string FileInfoListJsonLegacy { get; set; }
    }
}