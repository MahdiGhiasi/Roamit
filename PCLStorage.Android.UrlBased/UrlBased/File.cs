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

namespace PCLStorage.Droid.UrlBased
{
    public class File : PCLStorage.IFile
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

        public string Path => url.ToString();

        public File(Context context, Android.Net.Uri url)
        {
            this.context = context;
            this.url = url;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task MoveAsync(string newPath, NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> OpenAsync(FileAccess fileAccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileAccess == FileAccess.Read)
            {
                return context.ContentResolver.OpenInputStream(url);
            }
            else
            {
                return context.ContentResolver.OpenOutputStream(url);
            }
        }

        public Task RenameAsync(string newName, NameCollisionOption collisionOption = NameCollisionOption.FailIfExists, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
