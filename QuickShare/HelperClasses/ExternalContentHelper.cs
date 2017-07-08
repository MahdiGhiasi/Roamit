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
                var items = await data.GetStorageItemsAsync();
                var files = items.Where(x => x is StorageFile).Select(x => x as StorageFile).ToList();
                string url = "";
                if ((files.Count == 1) &&
                    ((files[0].FileType.ToLower() == ".html") /* Edge */ || (files[0].FileType.ToLower() == ".url") /* Chrome + Firefox */) &&
                    ((url = await IsALink(files[0])) != ""))
                {
                    SendDataTemporaryStorage.LaunchUri = new Uri(url);
                    SendDataTemporaryStorage.Text = url;
                    type = StandardDataFormats.WebLink;
                }
                /**
                else if ((files.Count == 0) && (items.Count == 1) && (items[0] is StorageFolder))
                {
                    SendDataTemporaryStorage.Files = new List<IStorageItem>(items);
                    type = StandardDataFormats.StorageItems;
                }
                /**/
                else
                {
                    SendDataTemporaryStorage.Files = new List<IStorageItem>(files);
                    type = StandardDataFormats.StorageItems;
                }
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

        private static async Task<string> IsALink(StorageFile file)
        {
            var properties = await file.GetBasicPropertiesAsync();
            if (properties.Size > 10*1024)
                return "";

            var text = await FileIO.ReadLinesAsync(file);

            if (text.Contains("[InternetShortcut]"))
            {
                int isId = text.IndexOf("[InternetShortcut]");
                for (int i = isId + 1; i < text.Count; i++)
                {
                    if (text[i].Substring(0, 4) == "URL=")
                    {
                        return text[i].Substring(4);
                    }
                }
            }

            return "";
        }

        internal static string SetUriData(Uri uri)
        {
            SendDataTemporaryStorage.LaunchUri = uri;
            SendDataTemporaryStorage.Text = uri.OriginalString;

            return StandardDataFormats.WebLink;
        }
    }
}
