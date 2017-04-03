using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickShare
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PicturePicker : Page
    {
        public ObservableCollection<PicturePickerItem> Items { get; set; } = new ObservableCollection<PicturePickerItem>();

        public PicturePicker()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadImagesAsync();
        }

        private async System.Threading.Tasks.Task LoadImagesAsync()
        {
            StorageFolder f = KnownFolders.PicturesLibrary;
            List<StorageFile> files = (await f.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.OrderByDate, 0, 50)).ToList();

            foreach (var file in files)
            {
                var thumbnailStream = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 140);

                if (thumbnailStream != null)
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(thumbnailStream);

                    Items.Add(new PicturePickerItem { File = file, Thumbnail = image });
                }
            }
        }

        private void SendButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            
        }

        private void BrowseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
