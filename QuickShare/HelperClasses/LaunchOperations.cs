using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace QuickShare.HelperClasses
{
    internal static class LaunchOperations
    {
        internal static async Task<bool> LaunchFolder(IStorageFolder folder)
        {
            return await Launcher.LaunchFolderAsync(folder);
        }

        internal static async Task<bool> LaunchFolderAndSelectItems(IStorageFolder folder, IEnumerable<IStorageItem> selectedItems)
        {
            FolderLauncherOptions options = new FolderLauncherOptions();
            foreach (var item in selectedItems)
            {
                options.ItemsToSelect.Add(item);
            }
            return await Launcher.LaunchFolderAsync(folder, options);
        }

        internal static async Task<bool> LaunchFolderFromPathAsync(string path)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            return await LaunchFolder(folder);
        }

        internal static async Task<bool> LaunchFolderFromPathAndSelectSingleItemAsync(string path, string fileName)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            var file = await folder.GetFileAsync(fileName);

            return await LaunchFolderAndSelectItems(folder, new IStorageItem[] { file });
        }

        internal static async Task<bool> LaunchFileFromPathAsync(string path, string fileName)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            var file = await folder.GetFileAsync(fileName);

            return await Launcher.LaunchFileAsync(file);
        }

        internal static async Task LaunchUrl(string url)
        {
            await Launcher.LaunchUriAsync(new Uri(url));
        }
    }
}
