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

namespace PCLStorage.Droid
{
    public class AndroidFolder : IFolder
    {
        private Context context;
        private DocumentFile folder;

        public string Name => folder.Name;
        public string Path => folder.Uri.ToString();

        public IFolder Parent { get; } = null;

        internal AndroidFolder(Context context, DocumentFile folder)
        {
            if (!folder.IsDirectory)
                throw new ArgumentException();

            this.context = context;
            this.folder = folder;
        }

        internal AndroidFolder(Context context, DocumentFile folder, IFolder parent)
            : this(context, folder)
        {
            Parent = parent;
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

        public async Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken))
        {
            desiredName = await EnsureExistence(desiredName, option, cancellationToken);
            var newFile = folder.CreateFile(GetMimeType(desiredName), desiredName);
            return new AndroidFile(context, newFile, this);
        }

        public async Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken = default(CancellationToken))
        {
            desiredName = await EnsureExistence(desiredName, option, cancellationToken);
            var newFolder = folder.CreateDirectory(desiredName);
            return new AndroidFolder(context, newFolder, this);
        }

        private async Task<string> EnsureExistence(string desiredName, CreationCollisionOption option, CancellationToken cancellationToken)
        {
            var existenceResult = await CheckExistsAsync(desiredName, cancellationToken);
            if (existenceResult != ExistenceCheckResult.NotFound)
            {
                if (option == CreationCollisionOption.FailIfExists)
                {
                    throw new AlreadyExistsException();
                }
                else if (option == CreationCollisionOption.ReplaceExisting)
                {
                    var file = await GetFileAsync(desiredName, cancellationToken);
                    await file.DeleteAsync(cancellationToken);
                }
                else if (option == CreationCollisionOption.OpenIfExists)
                {
                    throw new NotSupportedException("CreationCollisionOption.OpenIfExists is not supported for CreateFileAsync()");
                }
                else if (option == CreationCollisionOption.GenerateUniqueName)
                {
                    var originalName = desiredName;
                    int counter = 1;

                    do
                    {
                        counter++;
                        desiredName = $"{System.IO.Path.GetFileNameWithoutExtension(originalName)} ({counter}){System.IO.Path.GetExtension(originalName)}";
                    } while (await CheckExistsAsync(desiredName, cancellationToken) != ExistenceCheckResult.NotFound);
                }
            }

            return desiredName;
        }


        public async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            folder.Delete();
        }

        public async Task<IFile> GetFileAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var file = folder.FindFile(name);

            if (!file.IsFile)
                return null;

            return new AndroidFile(context, file, this);
        }

        public async Task<IFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var file = folder.FindFile(name);

            if (!file.IsDirectory)
                return null;

            return new AndroidFolder(context, file, this);
        }

        public async Task<IList<IFile>> GetFilesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return folder.ListFiles()
                .Where(x => x.IsFile)
                .Select(x => new AndroidFile(context, x, this))
                .Select(x => (IFile)x)
                .ToList();
        }

        public async Task<IList<IFolder>> GetFoldersAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return folder.ListFiles()
                .Where(x => x.IsDirectory)
                .Select(x => new AndroidFolder(context, x, this))
                .Select(x => (IFolder)x)
                .ToList();
        }

        public async Task<IList<IStorageItem>> GetItemsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var files = await GetFilesAsync(cancellationToken);
            var folders = await GetFoldersAsync(cancellationToken);

            return files.OfType<IStorageItem>().Concat(folders.OfType<IStorageItem>()).ToList();
        }
    }
}