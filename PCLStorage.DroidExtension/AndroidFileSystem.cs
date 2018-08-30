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
using Android.Widget;

namespace PCLStorage.Droid
{
    public class AndroidFileSystem : IFileSystem
    {
        readonly Context context;

        public IFolder LocalStorage { get; }
        public IFolder RoamingStorage => throw new NotSupportedException();

        public AndroidFileSystem(Context context)
        {
            this.context = context;

            LocalStorage = new AndroidFolder(context, GetDocumentFile(context.GetExternalFilesDir(null).AbsolutePath));
        }

        public async Task<IFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await FileSystem.Current.GetFileFromPathAsync(path, cancellationToken);
            }
            catch
            {
                try
                {
                    var item = GetDocumentFile(path);
                    return new AndroidFile(context, item);
                }
                catch
                {
                    return new AndroidUriFile(context, Android.Net.Uri.Parse(path));
                }
            }
        }

        public async Task<IFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await FileSystem.Current.GetFolderFromPathAsync(path, cancellationToken);
            }
            catch
            {
                var item = GetDocumentFile(path);
                return new AndroidFolder(context, item);
            }
        }

        public async Task<IStorageItem> GetItemFromPathAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await FileSystem.Current.GetFileFromPathAsync(path, cancellationToken);
            }
            catch
            {
                try
                {
                    return await FileSystem.Current.GetFolderFromPathAsync(path, cancellationToken);
                }
                catch
                {
                    try
                    {
                        var item = GetDocumentFile(path);

                        if (item.IsFile)
                            return new AndroidFile(context, item);
                        else
                            return new AndroidFolder(context, item);
                    }
                    catch
                    {
                        return new AndroidUriFile(context, Android.Net.Uri.Parse(path));
                    }
                }
            }
        }

        private DocumentFile GetDocumentFile(string path)
        {
            if (path.Contains("://"))
                return DocumentFile.FromTreeUri(context, Android.Net.Uri.Parse(path));
            else
                return DocumentFile.FromFile(new Java.IO.File(path));
        }
    }
}