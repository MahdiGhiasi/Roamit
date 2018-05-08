using QuickShare.HelperClasses;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace QuickShare.ViewModels.PicturePicker
{
    public class PicturePickerItem : INotifyPropertyChanged
    {
        public StorageFile File { get; set; }
        public bool IsAvailable { get => File.IsLocallyAvailable(); }
        public string ToolTipText
        {
            get
            {
                return File.DisplayName;
                //return File.DisplayName + "\r\n" +
                //       (IsAvailable ? "Available on this device" : "Available online only");
            }
        }

        bool thumbnailAbsent = false;
        BitmapImage thumbnail = null;
        public BitmapImage Thumbnail
        {
            get
            {
                TryLoadThumbnail();
                return thumbnail;
            }
        }

        public async Task TryLoadThumbnail()
        {
            if (thumbnail != null || thumbnailAbsent)
                return;

            Windows.Storage.FileProperties.StorageItemThumbnail thumbnailStream = null;

            try
            {
                thumbnailStream = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.PicturesView);
            }
            catch (Exception)
            {
                //thumbnailStream = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
            }

            if (thumbnailStream != null)
            {
                var image = new BitmapImage();
                await image.SetSourceAsync(thumbnailStream);

                thumbnail = image;

                OnPropertyChanged("Thumbnail");
            }
            else
            {
                thumbnailAbsent = true;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch { }
        }
    }
}