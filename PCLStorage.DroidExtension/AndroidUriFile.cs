using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.Provider;
using PCLStorage;

namespace PCLStorage.Droid
{
    public class AndroidUriFile : IFile, IStorageItem
    {
        private Context context;
        private Android.Net.Uri url;

        public string Name
        {
            get
            {
                Android.Database.ICursor cursor = null;
                try
                {
                    cursor = context.ContentResolver.Query(url, null, null, null, null);
                    if (cursor != null && cursor.MoveToFirst())
                    {
                        var displayName = cursor.GetString(cursor.GetColumnIndex(OpenableColumns.DisplayName));
                        return displayName;
                    }
                }
                finally
                {
                    cursor?.Close();
                }
                return "";
            }
        }

        public string Path => System.IO.Path.GetDirectoryName(url.ToString()).Replace('\\', '/');

        public AndroidUriFile(Context context, Android.Net.Uri url)
        {
            this.context = context;
            this.url = url;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task MoveAsync(string newPath, NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public async Task<Stream> OpenAsync(FileAccess fileAccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileAccess == FileAccess.Read)
            {
                var readStream = context.ContentResolver.OpenInputStream(url);
                var stream = await CopiedReadOnlyFileStream.CreateForStream(readStream);

                return stream;
            }
            else
            {
                return context.ContentResolver.OpenOutputStream(url);
            }
        }

        public Task RenameAsync(string newName, NameCollisionOption collisionOption = NameCollisionOption.FailIfExists, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        private long GetFileSize()
        {
            Android.Database.ICursor cursor = null;
            try
            {
                cursor = context.ContentResolver.Query(url, null, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    var sizeIndex = cursor.GetColumnIndex(OpenableColumns.Size);

                    if (cursor.IsNull(sizeIndex))
                        return 0; //Unknown file size

                    return long.Parse(cursor.GetString(sizeIndex));
                }
            }
            finally
            {
                cursor?.Close();
            }

            throw new Exception($"Unable to get file size for '{url.ToString()}'.");
        }

        public async Task<IFileStats> GetFileStats(CancellationToken cancellationToken = default(CancellationToken))
        {
            var name = Name;
            var length = GetFileSize();

            return new AndroidFileStats
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(name),
                Extension = System.IO.Path.GetExtension(name),
                Length = length,
            };
        }

        public Task SetCreationTime(DateTime creationTime, bool utc = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task SetLastAccessTime(DateTime lastAccessTime, bool utc = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public Task SetLastWriteTime(DateTime lastWriteTime, bool utc = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }
    }
}
