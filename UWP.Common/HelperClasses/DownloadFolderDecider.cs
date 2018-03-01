using PCLStorage;
using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    public static class DownloadFolderDecider
    {
        public static IAsyncOperation<IFolder> DecideAsync(string[] fileTypes)
        {
            return Decide(fileTypes).AsAsyncOperation();
        }

        private static async Task<IFolder> Decide(string[] fileTypes)
        {
            IStorageFolder folder;
            bool typeBasedDownloadFolder = (ApplicationData.Current.LocalSettings.Values.ContainsKey("TypeBasedDownloadFolder")) ? ((ApplicationData.Current.LocalSettings.Values["TypeBasedDownloadFolder"] as bool?) ?? false) : false;
            if (typeBasedDownloadFolder)
                folder = await DownloadFolderHelper.GetAppropriateDownloadFolderAsync(fileTypes);
            else
                folder = await DownloadFolderHelper.GetDefaultDownloadFolderAsync();

            return new WinRTFolder(folder);
        }

    }
}
