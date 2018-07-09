using System;
using System.Collections.Generic;
using System.IO;
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

namespace PCLStorage.Droid.UrlBased.DocumentFileBased
{
    class File : IFile
    {
        private Context context;
        private DocumentFile file;

        public string Name => file.Name;

        public string Path => file.ParentFile.Uri.ToString();

        public File(Context context, DocumentFile file)
        {
            if (!file.IsFile)
                throw new ArgumentException();

            this.context = context;
            this.file = file;
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            file.Delete();
        }

        public async Task MoveAsync(string newPath, NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> OpenAsync(FileAccess fileAccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileAccess == FileAccess.Read)
            {
                return context.ContentResolver.OpenInputStream(file.Uri);
            }
            else
            {
                return context.ContentResolver.OpenOutputStream(file.Uri);
            }
        }

        public async Task RenameAsync(string newName, NameCollisionOption collisionOption = NameCollisionOption.FailIfExists, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool alreadyExists = false;
            string finalName = newName;
            int counter = 1;

            do
            {
                counter++;
                alreadyExists = file.ParentFile.FindFile(finalName) != null;

                if (alreadyExists)
                {
                    switch (collisionOption)
                    {
                        case NameCollisionOption.GenerateUniqueName:
                            finalName = $"{System.IO.Path.GetFileNameWithoutExtension(newName)} ({counter}){System.IO.Path.GetExtension(newName)}";
                            break;
                        case NameCollisionOption.ReplaceExisting:
                            file.ParentFile.FindFile(finalName).Delete();
                            break;
                        case NameCollisionOption.FailIfExists:
                            return;
                        default:
                            break;
                    }
                }
            } while (alreadyExists);

            file.RenameTo(newName);
        }
    }
}