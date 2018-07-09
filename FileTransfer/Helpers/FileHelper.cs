using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer.Helpers
{
    internal static class FileHelper
    {
        internal static async Task<IFile> CreateFile(IFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
        }

        internal static async Task<IFile> GetFile(IFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }

        internal static async Task<IFolder> CreateDirectoryIfNecessary(IFolder downloadMainFolder, string directory)
        {
            string[] directories = directory.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (directories.Count() == 0)
                return downloadMainFolder;

            IFolder curFolder = downloadMainFolder;
            for (int i = 0; i < directories.Count(); i++)
            {
                curFolder = await curFolder.CreateFolderAsync(directories[i], CreationCollisionOption.OpenIfExists);
            }

            return curFolder;
        }
    }
}
