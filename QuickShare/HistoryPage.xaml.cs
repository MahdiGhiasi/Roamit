using GoogleAnalytics;
using QuickShare.Classes;
using QuickShare.Classes.ItemSources;
using QuickShare.HelperClasses;
using QuickShare.ViewModels.History;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using QuickShare.DataStore;
using Windows.Storage;
using System.Threading.Tasks;

namespace QuickShare
{
    public sealed partial class HistoryPage : Page
    {
        public IncrementalLoadingCollection<HistoryItemSource, HistoryItem> HistoryItems { get; set; }
        List<Guid> itemsToBeRemoved = new List<Guid>();

        public HistoryPage()
        {
            this.InitializeComponent();
            InitHistoryItems();
        }

        private void InitHistoryItems()
        {
            HistoryItems = new IncrementalLoadingCollection<HistoryItemSource, HistoryItem>(10, 1);
            HistoryItems.LoadFinished -= HistoryItems_LoadFinished;
            HistoryItems.LoadFinished += HistoryItems_LoadFinished;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Frame.BackStackDepth > 0)
                if (Frame.BackStack[Frame.BackStackDepth - 1].SourcePageType == typeof(HistoryPage))
                    Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);

            Window.Current.VisibilityChanged += Window_VisibilityChanged;

            base.OnNavigatedTo(e);
        }

        private void Window_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (e.Visible)
            {
                ReloadList();
            }
            else
            {
                SaveHistoryChanges();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            SaveHistoryChanges();

            base.OnNavigatingFrom(e);
        }

        private async void SaveHistoryChanges()
        {
            await DataStorageProviders.HistoryManager.OpenAsync();

            foreach (var item in itemsToBeRemoved)
                DataStorageProviders.HistoryManager.Remove(item);
            itemsToBeRemoved.Clear();

            DataStorageProviders.HistoryManager.Close();
        }

        private void ReloadList()
        {
            itemsToBeRemoved.Clear();
            HistoryItems.Clear();

            Frame.Navigate(typeof(HistoryPage));
        }

        private void HistoryItems_LoadFinished(EventArgs e)
        {
            if (HistoryItems.Count == 0)
            {
                HistoryEmptyNotice.Visibility = Visibility.Visible;
                ClearHistoryButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            App.Tracker.Send(HitBuilder.CreateScreenView("HistoryPage").Build());
#endif
        }

        private async void CopyToClipboard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                DataPackage content = new DataPackage();
                content.SetText(((HyperlinkButton)sender).Tag.ToString());

                Clipboard.SetContent(content);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageDialog md = new MessageDialog(ex.Message, "Can't write to clipboard.");
                await md.ShowAsync();
            }
        }

        private async void OpenSingleFile_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var info = (ViewModels.History.FileInfo)(((Button)sender).Tag);

            try
            {
                await LaunchOperations.LaunchFileFromPathAsync(info.Path, info.FileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                await (new MessageDialog("File not found.")).ShowAsync();
            }
            catch (UnauthorizedAccessException)
            {
                await (new MessageDialog($"We're sorry, but we can't access this file.\r\nTry finding it manually on File Explorer in '{info.Path}'")).ShowAsync();
            }
        }

        private async void OpenSingleFileContainingFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var info = (ViewModels.History.FileInfo)(((Button)sender).Tag);

            try
            {               
                await LaunchOperations.LaunchFolderFromPathAndSelectSingleItemAsync(info.Path, info.FileName);
            }
            catch (System.IO.FileNotFoundException)
            {
                await (new MessageDialog("File or folder does not exist.")).ShowAsync();
            }
            catch (UnauthorizedAccessException)
            {
                await (new MessageDialog($"We're sorry, but we can't access this folder.\r\nTry finding it manually on File Explorer in '{info.Path}'")).ShowAsync();
            }
        }

        private async void OpenFolder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var folder = ((HyperlinkButton)sender).Tag.ToString();
            try
            {
                await LaunchOperations.LaunchFolderFromPathAsync(folder);
            }
            catch (System.IO.FileNotFoundException)
            {
                await (new MessageDialog("File or folder does not exist.")).ShowAsync();
            }
            catch (UnauthorizedAccessException)
            {
                await (new MessageDialog($"We're sorry, but we can't access this folder.\r\nTry finding it manually on File Explorer in '{folder}'")).ShowAsync();
            }
        }
        
        private async void OpenLink_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var path = ((HyperlinkButton)sender).Tag.ToString();

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
            {
                await Launcher.LaunchUriAsync(uri);
            }
            else
            {
                await (new MessageDialog("Invalid url.", "Can't open link")).ShowAsync();
            }
        }

        private async void ClearHistory_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dlg = new MessageDialog("Are you sure you want to clear receive history?");
            dlg.Commands.Add(new UICommand
            {
                Label = "Yes",
                Id = 0,
            });
            dlg.Commands.Add(new UICommand
            {
                Label = "No",
                Id = 1,
            });
            dlg.DefaultCommandIndex = 0;
            dlg.CancelCommandIndex = 1;

            var result = await dlg.ShowAsync();

            if ((result.Id as int?) == 0)
            {
                ClearHistory();
            }
        }

        private async void ClearHistory()
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Clear();
            DataStorageProviders.HistoryManager.Close();

            ReloadList();
        }

        private void RemoveItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var tag = ((Control)sender).Tag as Guid?;

            if (!tag.HasValue)
                return;

            itemsToBeRemoved.Add(tag.Value);
            HistoryItems.Remove(HistoryItems.First(x => x.Guid == tag.Value));
        }
    }
}
