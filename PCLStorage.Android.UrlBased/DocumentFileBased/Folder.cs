using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Provider;
using Android.Views;
using Android.Webkit;
using Android.Widget;

namespace PCLStorage.Android.UrlBased.DocumentFileBased
{
    class Folder : IFolder
    {
        private DocumentFile folder;

        public string Name => folder.Name;

        public string Path => folder.ParentFile.Uri.ToString();

        public Folder(DocumentFile folder)
        {
            if (!folder.IsDirectory)
                throw new ArgumentException();

            this.folder = folder;
        }

        public async Task<ExistenceCheckResult> CheckExistsAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var selectedFile = folder.FindFile(name);
            if (selectedFile == null)
                return ExistenceCheckResult.NotFound;
            else if (selectedFile.IsFile)
                return ExistenceCheckResult.FileExists;
            else
                return ExistenceCheckResult.FolderExists;
        }

        private string GetMimeType(string file)
        {
            string type = null;
            string extension = System.IO.Path.GetExtension(file).Substring(1);
            if (extension != null)
            {
                type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            }
            
            if (type == null)
                type = "*/*";

            return type;
        }

        public Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken))
        {

            folder.CreateFile(GetMimeType(desiredName), desiredName);
        }

        public Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            folder.Delete();
        }

        public Task<IFile> GetFileAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<IFile>> GetFilesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IList<IFolder>> GetFoldersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}