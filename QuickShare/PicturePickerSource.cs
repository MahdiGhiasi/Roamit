using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace QuickShare
{
    public class PicturePickerSource : IIncrementalSource<PicturePickerItem>
    {
        public async Task<IEnumerable<PicturePickerItem>> GetPagedItems(int pageIndex, int pageSize)
        {
            StorageFolder f = KnownFolders.PicturesLibrary;
            List<StorageFile> files = (await f.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate, (uint)(pageIndex * pageSize), (uint)pageSize)).ToList();
            List<PicturePickerItem> result = new List<PicturePickerItem>();


            foreach (var file in files)
            {
                var thumbnailStream = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 140);

                if (thumbnailStream != null)
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(thumbnailStream);

                    result.Add(new PicturePickerItem { File = file, Thumbnail = image });
                }
            }

            return result;
        }
    }
}
