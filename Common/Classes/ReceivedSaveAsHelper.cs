using PCLStorage;
using QuickShare.DataStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Classes
{
    public static class ReceivedSaveAsHelper
    {
        public delegate void ReceivedSaveAsProgressEventHandler(double percent);
        public static event ReceivedSaveAsProgressEventHandler SaveAsProgress;

        public static async Task SaveAs(Guid guid, IFolder selectedFolder, string defaultDownloadFolder, Func<string, Task<IFile>> pathToFileConverter, Func<string, Task<IFolder>> pathToFolderConverter)
        {
            HistoryRow hr;
            await DataStorageProviders.HistoryManager.OpenAsync();
            hr = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();

            var files = (hr.Data as ReceivedFileCollection);
            var rootPath = files.StoreRootPath;

            Dictionary<ReceivedFile, IFile> fileMap = new Dictionary<ReceivedFile, IFile>();

            // Make sure all files are still there
            foreach (var item in files.Files)
            {
                try
                {
                    fileMap.Add(item, await pathToFileConverter(System.IO.Path.Combine(item.StorePath, item.Name)));
                }
                catch (Exception ex)
                {
                    throw new SaveAsFailedException("Couldn't move received files to the desired location", $"{ex.Message}: {item.StorePath}/{item.Name}");
                }
            }

            // Move files
            int cur = 0;
            int total = files.Files.Count;
            foreach (var item in files.Files)
            {
                IFolder dest = selectedFolder;
                string relativePath = "";

                if (item.StorePath.Length >= rootPath.Length)
                {
                    relativePath = item.StorePath.Substring(rootPath.Length);

                    if ((relativePath.Length > 0) && (relativePath[0] == '\\' || relativePath[0] == '/'))
                        relativePath = relativePath.Substring(1);

                    if (relativePath.Length > 0)
                    {
                        string[] pathParts = relativePath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var fName in pathParts)
                        {
                            var existence = await dest.CheckExistsAsync(fName);

                            if (existence != ExistenceCheckResult.FolderExists)
                                dest = await dest.CreateFolderAsync(fName, CreationCollisionOption.OpenIfExists);
                            else
                                dest = await dest.GetFolderAsync(fName);
                        }
                    }
                }

                var finalName = fileMap[item].Name;
                int i = 2;

                var fileExists = await dest.CheckExistsAsync(finalName);
                while (fileExists == ExistenceCheckResult.FileExists)
                {
                    finalName = $"{System.IO.Path.GetFileNameWithoutExtension(fileMap[item].Name)} ({i}){System.IO.Path.GetExtension(fileMap[item].Name)}";
                    fileExists = await dest.CheckExistsAsync(finalName);
                    i++;
                }

                await fileMap[item].MoveAsync(Path.Combine(dest.Path, finalName));
                item.StorePath = System.IO.Path.Combine(selectedFolder.Path, relativePath);
                item.Name = finalName;
                cur++;
                SaveAsProgress?.Invoke(((double)cur) / total);
            }
            //Delete old folder if necessary
            var rootFolder = await pathToFolderConverter(files.StoreRootPath);
            if (rootFolder.Path != defaultDownloadFolder)
            {
                //if ((await rootFolder.GetBasicPropertiesAsync()).Size == 0)
                if ((await rootFolder.GetFoldersAsync()).Count == 0 && (await rootFolder.GetFilesAsync()).Count == 0)
                {
                    await rootFolder.DeleteAsync();// StorageDeleteOption.PermanentDelete);
                }
            }

            //Set new StoreRootPath
            files.StoreRootPath = selectedFolder.Path;

            //Update database
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Remove(guid);
            DataStorageProviders.HistoryManager.Add(hr.Id, hr.ReceiveTime, hr.RemoteDeviceName, hr.Data, hr.Completed);
            DataStorageProviders.HistoryManager.Close();
        }
    }
}
