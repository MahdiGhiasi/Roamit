using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace QuickShare
{
    enum ClipboardContentType
    {
        Text, Bitmap, StorageItem, None
    }

    public sealed partial class MainActions : Page
    {
        ClipboardContentType currentContent = ClipboardContentType.None;
        bool isApplicationWindowActive = true;
        bool needToPrintClipboardFormat = false;
        string clipboardTextContent = "";

        private async Task InitClipboardAsync()
        {
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Window.Current.Activated += Window_Activated;

            await HandleClipboardChangedAsync();
        }

        private void GoodbyeClipboard()
        {
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            Window.Current.Activated -= Window_Activated;
        }

        private async Task HandleClipboardChangedAsync()
        {
            if (this.isApplicationWindowActive)
            {
                await ProcessClipboardAsync();
            }
            else
            {
                // Background applications can't access clipboard
                // Deferring processing of update notification until the application returns to foreground
                this.needToPrintClipboardFormat = true;
            }
        }

        private async Task ProcessClipboardAsync(int retryCount = 2)
        {
            try
            {
                var content = Clipboard.GetContent();
                if (content.Contains(StandardDataFormats.Bitmap))
                {
                    currentContent = ClipboardContentType.Bitmap;

                    SetClipboardPreviewText("(image)");
                }
                else if ((content.Contains(StandardDataFormats.StorageItems)) && ((await content.GetStorageItemsAsync()).FirstOrDefault(x => x is StorageFile) != null))
                {
                    currentContent = ClipboardContentType.StorageItem;

                    SetClipboardPreviewText("(file)");
                }
                else if (content.Contains(StandardDataFormats.Text))
                {
                    currentContent = ClipboardContentType.Text;
                    string text = await content.GetTextAsync();

                    SetClipboardPreviewText(text);
                }
                else
                {
                    //Unknown clipboard content.
                    SetClipboardPreviewText("");
                }
            }
            catch (Exception ex)
            {
                SetClipboardPreviewText("");
                Debug.WriteLine($"Failed to access clipboard: {ex.ToString()}");

                if (retryCount > 0)
                {
                    Debug.WriteLine("Will retry");
                    await Task.Delay(1000);
                    await ProcessClipboardAsync(retryCount - 1);
                }
            }
        }

        private void SetClipboardPreviewText(string text)
        {
            if (text.Length == 0)
            {
                ClipboardButton.IsEnabled = false;
                ClipboardContentPreviewContainer.Visibility = Visibility.Collapsed;
                return;
            }

            ClipboardButton.IsEnabled = true;
            ClipboardContentPreviewContainer.Visibility = Visibility.Visible;

            clipboardTextContent = text;

            if (text.Length > 61)
                ClipboardTextPreview.Text = text.Substring(0, 60).Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ") + "...";
            else
                ClipboardTextPreview.Text = text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");

            bool isValidUri = Uri.TryCreate(text, UriKind.Absolute, out _);
            if (isValidUri)
                ClipboardLaunchUrlButton.Visibility = Visibility.Visible;
            else
                ClipboardLaunchUrlButton.Visibility = Visibility.Collapsed;
        }

        private async void Window_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            this.isApplicationWindowActive = (e.WindowActivationState != CoreWindowActivationState.Deactivated);
            if (this.needToPrintClipboardFormat)
            {
                // The clipboard was updated while the sample was in the background. If the sample is now in the foreground, 
                // handle the new content. 
                await HandleClipboardChangedAsync();
            }
        }

        private async void Clipboard_ContentChanged(object sender, object e)
        {
            await HandleClipboardChangedAsync();
        }

        public async Task<IEnumerable<IStorageItem>> GetStorageItemsFromClipboardAsync()
        {
            List<IStorageItem> output = new List<IStorageItem>();
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.StorageItems))
                return output;

            var items = await content.GetStorageItemsAsync();
            output.AddRange(items.Where(x => x is StorageFile));
            return output;
        }

        public async Task<StorageFile> GetBitmapFromClipboardAsync()
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Bitmap))
                return null;

            IRandomAccessStreamReference imageReceived = null;
            imageReceived = await content.GetBitmapAsync();

            if (imageReceived == null)
                return null;

            string name = $"Screenshot {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.png";
            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("ClipboardTemp", CreationCollisionOption.OpenIfExists);

            return await SaveToPngTaskFile(imageReceived, folder, name);
        }

        //From https://stackoverflow.com/a/25661877/942659
        public static async Task<StorageFile> SaveToPngTaskFile(IRandomAccessStreamReference rndAccessStreamReference, StorageFolder storageFolder, string storageFileName)
        {
            IRandomAccessStreamWithContentType rndAccessStreamWithContentType = await rndAccessStreamReference.OpenReadAsync();
            StorageFile storageFile = await storageFolder.CreateFileAsync(storageFileName, CreationCollisionOption.GenerateUniqueName);
            var decoder = await BitmapDecoder.CreateAsync(rndAccessStreamWithContentType);
            var pixels = await decoder.GetPixelDataAsync();
            var outStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
            encoder.SetPixelData(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore,
                decoder.OrientedPixelWidth, decoder.OrientedPixelHeight,
                decoder.DpiX, decoder.DpiY,
                pixels.DetachPixelData());
            await encoder.FlushAsync();
            outStream.Dispose();
            return storageFile;
        }
    }
}
