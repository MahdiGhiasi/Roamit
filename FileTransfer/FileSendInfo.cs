using Newtonsoft.Json;
using PCLStorage;
using System.Linq;
using System;
using System.Collections.Generic;
using QuickShare.Common;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    public class FileSendInfo
    {
        readonly int fileKeyLength = 20;

        private static HashSet<string> usedKeys = new HashSet<string>();

        [JsonIgnore]
        public IFile File { get; }

        [JsonProperty]
        public string RelativePath { get; private set; }
        [JsonProperty]
        public string UniqueKey { get; private set; }
        [JsonProperty]
        public string FileName { get; private set; }
        [JsonProperty]
        public uint SlicesCount { get; private set; }
        [JsonProperty]
        public ulong LastSliceSize { get; private set; }
        [JsonProperty]
        public ulong SliceMaxLength { get; private set; }
        [JsonProperty]
        public ulong FileSize { get; private set; }

        public FileSendInfo() { }

        /// <summary>
        /// Initiate a FileSendInfo but ignore the folder structure.
        /// </summary>
        /// <param name="file"></param>
        public FileSendInfo(IFile file)
        {
            File = file;
            FileName = file.Name;
            RelativePath = "";
            UniqueKey = GenerateKey();
        }

        /// <summary>
        /// Initiate a FileSendInfo while preserving the folder structure relative to 'parentPath'.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="parentPath"></param>
        public FileSendInfo(IFile file, string parentPath) :
            this(file)
        {
            if ((parentPath.LastOrDefault() != '\\') && (parentPath.LastOrDefault() != '/'))
                parentPath += "/";

            if (file.Path.Substring(0, parentPath.Length).Replace("\\", "/") == parentPath.Replace("\\", "/"))
                RelativePath = System.IO.Path.GetDirectoryName(file.Path).Substring(parentPath.Length - 1).Replace("\\", "/");
            else
                throw new ArgumentException("'parentPath' is not a part of 'file.Path'.");
        }

        private string GenerateKey()
        {
            string key;
            do
            {
                key = RandomFunctions.RandomString(fileKeyLength);
            } while (usedKeys.Contains(key));
            usedKeys.Add(key);

            return key;
        }

        public async Task InitSlicingAsync()
        {
            var properties = await File.GetFileStats();
            FileSize = (ulong)properties.Length;
            SlicesCount = (uint)Math.Ceiling(((double)properties.Length) / ((double)Constants.FileSliceMaxLength));
            SliceMaxLength = Constants.FileSliceMaxLength;

            LastSliceSize = ((ulong)properties.Length % Constants.FileSliceMaxLength);
            if (LastSliceSize == 0)
                LastSliceSize = Constants.FileSliceMaxLength;
        }
    }
}