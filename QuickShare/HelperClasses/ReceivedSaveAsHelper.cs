using QuickShare.Common;
using QuickShare.DataStore;
using QuickShare.ToastNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace QuickShare.HelperClasses
{
    internal static class ReceivedSaveAsHelper
    {
        public delegate void ReceivedSaveAsProgressEventHandler(double percent);
        public static event ReceivedSaveAsProgressEventHandler SaveAsProgress;

        public static async Task SaveAs(Guid guid)
        {
            //Windows 10 Mobile has a bug causing PickSingleFolderAsync() to throw exception if called immediately after launch.
            if (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone)
                await Task.Delay(TimeSpan.FromSeconds(1.5));

            FolderPicker fp = new FolderPicker()
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            fp.FileTypeFilter.Add("*");
            var selectedFolder = await fp.PickSingleFolderAsync();

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            FutureAccessListHelper.MakeSureFutureAccessListIsNotFull();
            futureAccessList.AddOrReplace(selectedFolder.Path.Replace(":", "").Replace('\\', '/'), selectedFolder);

            await SaveAs(guid, selectedFolder);
        }

        private static async Task SaveAs(Guid guid, StorageFolder selectedFolder)
        {
            HistoryRow hr;
            await DataStorageProviders.HistoryManager.OpenAsync();
            hr = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();

            var files = (hr.Data as ReceivedFileCollection);
            var rootPath = files.StoreRootPath;

            Dictionary<ReceivedFile, StorageFile> fileMap = new Dictionary<ReceivedFile, StorageFile>();

            // Make sure all files are still there
            foreach (var item in files.Files)
            {
                try
                {
                    fileMap.Add(item, await StorageFile.GetFileFromPathAsync(System.IO.Path.Combine(item.StorePath, item.Name)));
                }
                catch (Exception ex)
                {
                    ToastFunctions.SendToast("Couldn't move received files to the desired location", item.StorePath + item.Name + ex.Message);
                    return;
                }
            }

            // Move files
            int cur = 0;
            int total = files.Files.Count;
            foreach (var item in files.Files)
            {
                StorageFolder dest = selectedFolder;
                string relativePath = "";

                if (item.StorePath.Length >= rootPath.Length)
                {
                    relativePath = item.StorePath.Substring(rootPath.Length);

                    if ((relativePath.Length > 0) && (relativePath[0] == '\\'))
                        relativePath = relativePath.Substring(1);

                    if (relativePath.Length > 0)
                    {
                        string[] pathParts = relativePath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var fName in pathParts)
                        {
                            var x = await dest.TryGetItemAsync(fName);

                            if ((x == null) || (!(x is StorageFolder)))
                                dest = await dest.CreateFolderAsync(fName);
                            else
                                dest = x as StorageFolder;
                        }
                    }
                }

                var finalName = fileMap[item].Name;
                int i = 2;
                var existingFile = await dest.TryGetItemAsync(finalName);
                while ((existingFile != null) && (existingFile is StorageFile))
                {
                    finalName = $"{System.IO.Path.GetFileNameWithoutExtension(fileMap[item].Name)} ({i}){System.IO.Path.GetExtension(fileMap[item].Name)}";
                    existingFile = await dest.TryGetItemAsync(finalName);
                    i++;
                }

                await fileMap[item].MoveAsync(dest, finalName);
                item.StorePath = System.IO.Path.Combine(selectedFolder.Path, relativePath);
                item.Name = finalName;
                cur++;
                SaveAsProgress?.Invoke(((double)cur) / total);
            }
            //Delete old folder if necessary
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(files.StoreRootPath);
            if (rootFolder.Path != (await DownloadFolderHelper.GetDefaultDownloadFolderAsync()).Path)
            {
                if ((await rootFolder.GetBasicPropertiesAsync()).Size == 0)
                {
                    await rootFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }

            //Set new StoreRootPath
            files.StoreRootPath = selectedFolder.Path;

            //Update database
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Remove(guid);
            DataStorageProviders.HistoryManager.Add(hr.Id, hr.ReceiveTime, hr.RemoteDeviceName, hr.Data, hr.Completed);
            DataStorageProviders.HistoryManager.Close();

            Toaster.ShowFileReceiveFinishedSavedAsNotification(hr.Id);
        }
    }
}
