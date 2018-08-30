using System;

namespace PCLStorage.Droid
{
    public class FileSystemFile : PCLStorage.FileSystemFile, IStorageItem
    {
        public FileSystemFile(string path) : base(path)
        {
        }
    }
}

