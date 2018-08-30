using System;

namespace PCLStorage.Droid
{
    public class FileSystemFolder : PCLStorage.FileSystemFolder, IStorageItem
    {
        public FileSystemFolder(string path) : base(path)
        {
        }

        public FileSystemFolder(string path, bool canDelete) : base(path, canDelete)
        {
        }
    }
}

