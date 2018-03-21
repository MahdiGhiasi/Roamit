using Newtonsoft.Json;
using PCLStorage;
using System.Linq;
using System;
using System.Collections.Generic;
using QuickShare.Common;
using System.Threading.Tasks;

namespace FileTransfer
{
    public class FileSendInfo
    {
        readonly int fileKeyLength = 20;

        private static HashSet<string> usedKeys = new HashSet<string>();

        [JsonIgnore]
        public IFile File { get; }

        public string FileName { get => File.Name; }
        public string RelativePath { get; }
        public string UniqueKey { get; }

        public uint SlicesCount { get; private set; }
        public ulong LastSliceSize { get; private set; }

        public FileSendInfo(IFile file, string parentPath)
        {
            File = file;

            if ((parentPath.LastOrDefault() != '\\') && (parentPath.LastOrDefault() != '/'))
                parentPath += "/";

            if (file.Path.Substring(0, parentPath.Length).Replace("\\", "/") == parentPath.Replace("\\", "/"))
                RelativePath = System.IO.Path.GetDirectoryName(file.Path).Substring(parentPath.Length - 1).Replace("\\", "/");
            else
                throw new ArgumentException("'parentPath' is not a part of 'file.Path'.");

            UniqueKey = GenerateKey();
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
            SlicesCount = (uint)Math.Ceiling(((double)properties.Length) / ((double)Constants.FileSliceMaxLength));

            LastSliceSize = ((ulong)properties.Length % Constants.FileSliceMaxLength);
            if (LastSliceSize == 0)
                LastSliceSize = Constants.FileSliceMaxLength;
        }
    }
}