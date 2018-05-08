using QuickShare.Common;
using QuickShare.ViewModels.PicturePicker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace QuickShare.Classes.ItemSources
{
    public class PicturePickerSource : IIncrementalSource<PicturePickerItem>
    {
        public static uint ThumbnailSize = (uint)(DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone ? 140 : 160);

        private List<PicturePickerItem> allItems = new List<PicturePickerItem>();
        private bool isFillingList = false;
        int lastPageIndexLoaded = 0;

        public async Task<IEnumerable<PicturePickerItem>> GetPagedItems(int pageIndex, int pageSize)
        {
            if (allItems.Count >= (pageIndex * pageSize + pageSize))
            {
                lastPageIndexLoaded++;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FillNextItemsToList(lastPageIndexLoaded, pageSize);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return allItems.Skip(pageIndex * pageSize).Take(pageSize);
            }
            else if (isFillingList)
            {
                while (isFillingList)
                    await Task.Delay(50);

                return await GetPagedItems(pageIndex, pageSize);
            }
            else
            {
                int currentCount = allItems.Count;
                while (allItems.Count < (pageIndex * pageSize + pageSize))
                {
                    await FillNextItemsToList(lastPageIndexLoaded, pageSize);
                    if (allItems.Count == currentCount) //We reached the end
                        break;
                    currentCount = allItems.Count;
                    lastPageIndexLoaded++;
                }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FillNextItemsToList(lastPageIndexLoaded, pageSize);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return allItems.Skip(pageIndex * pageSize).Take(pageSize);
            }
        }

        private async Task FillNextItemsToList(int pageIndex, int pageSize)
        {
            isFillingList = true;

            StorageFolder f = KnownFolders.PicturesLibrary;
            List<StorageFile> files = (await f.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate, (uint)(pageIndex * pageSize), (uint)pageSize)).ToList();
            List<PicturePickerItem> result = new List<PicturePickerItem>();

            if ((files.Count < pageSize) && (allItems.Count > (pageIndex * pageSize))) //This is the end
            {
                isFillingList = false;
                return;
            }
            else if (files.Count == 0) //We already reached the end
            {
                lastPageIndexLoaded--;
                isFillingList = false;
                return;
            }

            foreach (var file in files)
            {
                result.Add(new PicturePickerItem { File = file });
            }
            allItems.AddRange(result);

            isFillingList = false;
        }
    }
}
