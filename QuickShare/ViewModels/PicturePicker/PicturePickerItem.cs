using QuickShare.HelperClasses;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace QuickShare.ViewModels.PicturePicker
{
    public class PicturePickerItem
    {
        public StorageFile File { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public bool IsAvailable { get => File.IsLocallyAvailable(); }
        public string ToolTipText
        {
            get
            {
                return File.DisplayName + "\r\n" +
                       (IsAvailable ? "Available on this device" : "Available online only");
            }
        }
    }
}