using PCLStorage;
using QuickShare.Common;
using QuickShare.Common.Classes;
using QuickShare.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    public class DownloadFolderDecider : IDownloadFolderDecider
    {
        public async Task<IFolder> DecideAsync(string[] fileTypes)
        {
            IStorageFolder folder;
            bool typeBasedDownloadFolder = (ApplicationData.Current.LocalSettings.Values.ContainsKey("TypeBasedDownloadFolder")) ? ((ApplicationData.Current.LocalSettings.Values["TypeBasedDownloadFolder"] as bool?) ?? false) : false;
            if (typeBasedDownloadFolder)
                folder = await DownloadFolderHelper.GetAppropriateDownloadFolderAsync(fileTypes);
            else
                folder = await GetGroupedDownloadFolder(await DownloadFolderHelper.GetDefaultDownloadFolderAsync(), DownloadGroupByHelper.GetState());

            return new WinRTFolder(folder);
        }

        private async Task<IStorageFolder> GetGroupedDownloadFolder(IStorageFolder storageFolder, DownloadGroupByItem state)
        {
            var folderName = state.Decider(DateTime.Now);
            if (folderName.Length == 0)
                return storageFolder;
            return await storageFolder.CreateFolderAsync(folderName, Windows.Storage.CreationCollisionOption.OpenIfExists);
        }
    }
}
