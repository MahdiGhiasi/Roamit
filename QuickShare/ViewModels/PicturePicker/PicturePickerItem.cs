using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace QuickShare.ViewModels.PicturePicker
{
    public class PicturePickerItem
    {
        public StorageFile File { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
}