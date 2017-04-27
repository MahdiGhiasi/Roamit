using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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

        private async Task ProcessClipboardAsync()
        {
            try
            {
                var content = Clipboard.GetContent();
                if (content.Contains(StandardDataFormats.Bitmap))
                {
                    currentContent = ClipboardContentType.Bitmap;

                    SetClipboardPreviewText("(image)");
                }
                else if (content.Contains(StandardDataFormats.StorageItems))
                {
                    currentContent = ClipboardContentType.StorageItem;

                    SetClipboardPreviewText("(file or folder)");
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
    }
}
