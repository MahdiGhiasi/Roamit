using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace QuickShare.Common
{
    public static class DownloadFolderHelper
    {
        internal static readonly string _downloadMainFolder = "downloadMainFolder";

        /**
        public static IAsyncAction InitDefaultDownloadFolderAsync()
        {
            return InitDefaultDownloadFolder().AsAsyncAction();
        }
        /**/

        /**
        public static IAsyncOperation<bool> DownloadFolderExistsAsync()
        {
            return DefaultDownloadFolderExists().AsAsyncOperation();
        }
        /**/

        public static IAsyncOperation<IStorageFolder> GetDefaultDownloadFolderAsync()
        {
            return GetDefaultDownloadFolder().AsAsyncOperation();
        }

        public static IAsyncOperation<IStorageFolder> TrySetDefaultDownloadFolderAsync(IStorageFolder folder)
        {
            return TrySetDownloadFolder(folder).AsAsyncOperation();
        }

        public static IAsyncOperation<IStorageFolder> GetAppropriateDownloadFolderAsync(string fileType)
        {
            return GetAppropriateDownloadFolder(new string[] { fileType }).AsAsyncOperation();
        }

        public static IAsyncOperation<IStorageFolder> GetAppropriateDownloadFolderAsync(string[] fileType)
        {
            return GetAppropriateDownloadFolder(fileType).AsAsyncOperation();
        }

        private static async Task InitDefaultDownloadFolder()
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            if (!(await DefaultDownloadFolderExists()))
            {
                bool created = false;
                int i = 1;
                do
                {
                    try
                    {
                        var myfolder = await DownloadsFolder.CreateFolderAsync((i == 1) ? "Received" : $"Received ({i})");

                        FutureAccessListHelper.MakeSureFutureAccessListIsNotFull();
                        futureAccessList.AddOrReplace(_downloadMainFolder, myfolder);
                        created = true;
                    }
                    catch
                    {
                        i++;
                    }
                }
                while (!created);
            }
        }

        private static async Task<bool> DefaultDownloadFolderExists()
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            try
            {
                if (!futureAccessList.ContainsItem(_downloadMainFolder))
                    return false;

                await futureAccessList.GetItemAsync(_downloadMainFolder);
                return true;
            }
            catch
            {
                futureAccessList.Remove(_downloadMainFolder);
                return false;
            }
        }

        private static async Task<IStorageFolder> GetDefaultDownloadFolder()
        {
            await InitDefaultDownloadFolder();

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            var folder = (await futureAccessList.GetItemAsync(_downloadMainFolder)) as IStorageFolder;
            return folder;
        }

        private static async Task<IStorageFolder> TrySetDownloadFolder(IStorageFolder folder)
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            FutureAccessListHelper.MakeSureFutureAccessListIsNotFull();
            futureAccessList.AddOrReplace(_downloadMainFolder, folder);

            return await GetDefaultDownloadFolder(); //Make sure everything's fine
        }

        private static async Task<IStorageFolder> GetAppropriateDownloadFolder(string[] fileType)
        {
            IEnumerable<FileTypeCategory> categories = fileType.Select(x => GetFileCategory(x));

            FileTypeCategory preferredCategory;
            if (categories.Distinct().Count() == 1)
                preferredCategory = categories.First();
            else
                preferredCategory = FileTypeCategory.General;

            switch (preferredCategory)
            {
                case FileTypeCategory.Video:
                    return await CreateOrGetRoamitFolder(KnownFolders.VideosLibrary);
                case FileTypeCategory.Picture:
                    return await CreateOrGetRoamitFolder(KnownFolders.PicturesLibrary);
                    //return KnownFolders.SavedPictures;
                case FileTypeCategory.Music:
                    return await CreateOrGetRoamitFolder(KnownFolders.MusicLibrary);
                default:
                    return await GetDefaultDownloadFolder();
            }
        }

        private static FileTypeCategory GetFileCategory(string fileType)
        {
            fileType = fileType.ToLower();
            switch (fileType)
            {
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".mkv":
                    return FileTypeCategory.Video;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return FileTypeCategory.Picture;
                case ".mp3":
                case ".m4a":
                case ".wav":
                case ".wma":
                    return FileTypeCategory.Music;
                default:
                    return FileTypeCategory.General;
            }
        }

        private static async Task<IStorageFolder> CreateOrGetRoamitFolder(StorageFolder parentFolder)
        {
            return await parentFolder.CreateFolderAsync("Roamit", CreationCollisionOption.OpenIfExists);
        }

        enum FileTypeCategory
        {
            Picture = 1,
            Video = 2,
            Music = 3,
            General = 4,
        }
    }
}
