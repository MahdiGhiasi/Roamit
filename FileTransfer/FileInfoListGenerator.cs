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

        internal async Task<string> GenerateAsync()
        {
            List<string> items = await GetQueueInfoItems();
            return JsonConvert.SerializeObject(items);
        }

        private async Task<List<string>> GetQueueInfoItems()
        {
            List<string> items = new List<string>();
            foreach (var item in files)
            {
                var key = item.UniqueKey;
                items.Add(GetQueueFileInfo(key, item.SlicesCount, item.FileName, await item.File.GetFileStats(), item.RelativePath));
            }

            return items;
        }

        private string GetQueueFileInfo(string key, uint slicesCount, string fileName, IFileStats properties, string directory)
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
}