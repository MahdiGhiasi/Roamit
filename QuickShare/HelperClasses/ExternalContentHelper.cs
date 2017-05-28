using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    internal static class ExternalContentHelper
    {
        internal static async Task<string> SetData(DataPackageView data)
        {
            string type = "";
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                SendDataTemporaryStorage.Files = (await data.GetStorageItemsAsync()).Where(x => x is StorageFile).Select(x => x as StorageFile).ToList();
                type = StandardDataFormats.StorageItems;
            }
            else if (data.Contains(StandardDataFormats.WebLink))
            {
                SendDataTemporaryStorage.LaunchUri = await data.GetWebLinkAsync();
                SendDataTemporaryStorage.Text = SendDataTemporaryStorage.LaunchUri.OriginalString;
                type = StandardDataFormats.WebLink;
            }
            else if (data.Contains(StandardDataFormats.ApplicationLink))
            {
                SendDataTemporaryStorage.LaunchUri = await data.GetApplicationLinkAsync();
                SendDataTemporaryStorage.Text = SendDataTemporaryStorage.LaunchUri.OriginalString;
                type = StandardDataFormats.ApplicationLink;
            }
            else if (data.Contains(StandardDataFormats.Text))
            {
                SendDataTemporaryStorage.Text = await data.GetTextAsync();
                type = StandardDataFormats.Text;
            }

            return type;
        }
    }
}
