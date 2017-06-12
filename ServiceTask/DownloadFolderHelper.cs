using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    public static class DownloadFolderHelper
    {
        public static async Task InitDownloadFolder()
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            if (!(await DownloadFolderExists()))
            {
                bool created = false;
                int i = 1;
                do
                {
                    try
                    {
                        var myfolder = await DownloadsFolder.CreateFolderAsync((i == 1) ? "Received" : $"Received ({i})");
                        futureAccessList.AddOrReplace("downloadMainFolder", myfolder);
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

        private static async Task<bool> DownloadFolderExists()
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            try
            {
                if (!futureAccessList.ContainsItem("downloadMainFolder"))
                    return false;

                await futureAccessList.GetItemAsync("downloadMainFolder");
                return true;
            }
            catch
            {
                futureAccessList.Remove("downloadMainFolder");
                return false;
            }
        }
    }
}
