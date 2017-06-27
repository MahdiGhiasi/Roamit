using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Provider;
using Android.Util;
using Uri = Android.Net.Uri;
using Android.Database;

namespace QuickShare.Droid.Helpers
{
    //Obtained from https://github.com/iPaulPro/aFileChooser/blob/master/aFileChooser/src/com/ipaulpro/afilechooser/utils/FileUtils.java
    internal static class FilePathHelper
    {
        /**
            * Get a file path from a Uri. This will get the the path for Storage Access
            * Framework Documents, as well as the _data field for the MediaStore and
            * other file-based ContentProviders.<br>
            * <br>
            * Callers should check whether the path is local before assuming it
            * represents a local file.
            * 
            * @param context The context.
            * @param uri The Uri to query.
            * @see #isLocal(string)
            * @see #getFile(Context, Uri)
            * @author paulburke
            */
        public static string GetPath(Context context, Uri uri)
        {

#if DEBUG
            Log.Debug("FilePathHelper -",
                        "Authority: " + uri.Authority +
                                ", Fragment: " + uri.Fragment +
                                ", Port: " + uri.Port +
                                ", Query: " + uri.Query +
                                ", Scheme: " + uri.Scheme +
                                ", Host: " + uri.Host +
                                ", Segments: " + uri.PathSegments.ToString()
                        );
#endif

            // DocumentProvider
            if (DocumentsContract.IsDocumentUri(context, uri))
            {
                // LocalStorageProvider
                if (IsLocalStorageDocument(uri))
                {
                    // The path is the id
                    return DocumentsContract.GetDocumentId(uri);
                }
                // ExternalStorageProvider
                else if (IsExternalStorageDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(':');
                    string type = split[0];

                    if ("primary".ToLower() == type.ToLower())
                    {
                        return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
                    }

                    // TODO handle non-primary volumes
                }
                // DownloadsProvider
                else if (IsDownloadsDocument(uri))
                {

                    string id = DocumentsContract.GetDocumentId(uri);
                    Uri contentUri = ContentUris.WithAppendedId(Uri.Parse("content://downloads/public_downloads"), long.Parse(id));

                    return GetDataColumn(context, contentUri, null, null);
                }
                // MediaProvider
                else if (IsMediaDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(':');
                    string type = split[0];

                    Uri contentUri = null;
                    if ("image" == type)
                    {
                        contentUri = MediaStore.Images.Media.ExternalContentUri;
                    }
                    else if ("video" == type)
                    {
                        contentUri = MediaStore.Video.Media.ExternalContentUri;
                    }
                    else if ("audio" == type)
                    {
                        contentUri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    string selection = "_id=?";
                    string[] selectionArgs = new string[] {
                        split[1]
                };

                    return GetDataColumn(context, contentUri, selection, selectionArgs);
                }
            }
            // MediaStore (and general)
            else if ("content".ToLower() == uri.Scheme.ToLower())
            {

                // Return the remote address
                if (IsGooglePhotosUri(uri))
                    return uri.LastPathSegment;

                return GetDataColumn(context, uri, null, null);
            }
            // File
            else if ("file".ToLower() == uri.Scheme.ToLower())
            {
                return uri.Path;
            }

            return null;
        }


        /**
          * @param uri The Uri to check.
          * @return Whether the Uri authority is {@link LocalStorageProvider}.
          * @author paulburke
          */
        private static bool IsLocalStorageDocument(Uri uri)
        {
            return false;
            //return LocalStorageProvider.AUTHORITY.equals(uri.Authority);
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is ExternalStorageProvider.
         * @author paulburke
         */
        private static bool IsExternalStorageDocument(Uri uri)
        {
            return "com.android.externalstorage.documents" == uri.Authority;
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is DownloadsProvider.
         * @author paulburke
         */
        private static bool IsDownloadsDocument(Uri uri)
        {
            return "com.android.providers.downloads.documents" == uri.Authority;
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is MediaProvider.
         * @author paulburke
         */
        private static bool IsMediaDocument(Uri uri)
        {
            return "com.android.providers.media.documents" == uri.Authority;
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is Google Photos.
         */
        private static bool IsGooglePhotosUri(Uri uri)
        {
            return "com.google.android.apps.photos.content" == uri.Authority;
        }


        /**
         * Get the value of the data column for this Uri. This is useful for
         * MediaStore Uris, and other file-based ContentProviders.
         *
         * @param context The context.
         * @param uri The Uri to query.
         * @param selection (Optional) Filter used in the query.
         * @param selectionArgs (Optional) Selection arguments used in the query.
         * @return The value of the _data column, which is typically a file path.
         * @author paulburke
         */
        private static string GetDataColumn(Context context, Uri uri, string selection, string[] selectionArgs)
        {

            ICursor cursor = null;
            string column = "_data";
            string[] projection = {
                column
            };

            try
            {
                cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs,
                        null);
                if (cursor != null && cursor.MoveToFirst())
                {
#if DEBUG
                    DatabaseUtils.DumpCursor(cursor);
#endif

                    int column_index = cursor.GetColumnIndexOrThrow(column);
                    return cursor.GetString(column_index);
                }
            }
            catch
            {

            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return null;
        }


    }
}