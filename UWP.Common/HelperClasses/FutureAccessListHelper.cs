using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public static class FutureAccessListHelper
    {
        public static void MakeSureFutureAccessListIsNotFull()
        {
            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            var allowedCount = (futureAccessList.MaximumItemsAllowed - futureAccessList.Entries.Count);
            if (allowedCount < 1)
            {
                //Remove one
                futureAccessList.Remove(futureAccessList.Entries.FirstOrDefault(x => x.Token != DownloadFolderHelper._downloadMainFolder).Token);
            }
        }

    }
}
