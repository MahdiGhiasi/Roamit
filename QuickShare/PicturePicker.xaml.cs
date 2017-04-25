using QuickShare.Common;
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
using Windows.Storage.Pickers;
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
        public ObservableCollection<PicturePickerItem> Items { get; set; } = MainPage.Current.PicturePickerItems;

        public PicturePicker()
        {
            this.InitializeComponent();
        }

        private void Send(IEnumerable<StorageFile> files)
        {
            SendDataTemporaryStorage.Files.Clear();
            SendDataTemporaryStorage.Files.AddRange(files);

            Frame.Navigate(typeof(MainSend), "file");
        }

        private void SendButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<StorageFile> files = (from x in gridView.SelectedItems
                                       select (x as PicturePickerItem).File).ToList();

            Send(files);
        }

        private async void BrowseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".mp4");

            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                Send(files);
            }
        }
    }
}
