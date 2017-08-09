using QuickShare.DataStore;
using QuickShare.ViewModels.History;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Classes.ItemSources
{
    public class HistoryItemSource : IIncrementalSource<HistoryItem>
    {
        public async Task<IEnumerable<HistoryItem>> GetPagedItems(int pageIndex, int pageSize)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            await DataStorageProviders.TextReceiveContentManager.OpenAsync();

            var data = DataStorageProviders.HistoryManager.GetPage(pageIndex * pageSize, pageSize)
                .Select(x => GenerateHistoryItemAsync(x)).ToList();

            DataStorageProviders.TextReceiveContentManager.Close();
            DataStorageProviders.HistoryManager.Close();

            return data;
        }

        private HistoryItem GenerateHistoryItemAsync(HistoryRow x)
        {
            if (x.Data is ReceivedUrl)
            {
                return new HistoryWebLinkItem
                {
                    Guid = x.Id,
                    ItemDateAndTime = x.ReceiveTime,
                    SenderName = x.RemoteDeviceName,
                    LinkPath = (x.Data as ReceivedUrl).Uri.OriginalString,
                };
            }
            else if (x.Data is ReceivedText)
            {
                var content = DataStorageProviders.TextReceiveContentManager.GetItemContent(x.Id);

                return new HistoryClipboardTextItem
                {
                    Guid = x.Id,
                    ItemDateAndTime = x.ReceiveTime,
                    SenderName = x.RemoteDeviceName,
                    Content = content,
                };
            }
            else if (x.Data is ReceivedFile)
            {
                var file = x.Data as ReceivedFile;
                return new HistorySingleFileItem
                {
                    Guid = x.Id,
                    ItemDateAndTime = x.ReceiveTime,
                    SenderName = x.RemoteDeviceName,
                    File = new FileInfo
                    {
                        FileName = file.Name,
                        Path = file.StorePath,
                    }
                };
            }
            else if (x.Data is ReceivedFileCollection)
            {
                var files = x.Data as ReceivedFileCollection;
                return new HistoryMultipleFileItem
                {
                    Guid = x.Id,
                    ItemDateAndTime = x.ReceiveTime,
                    SenderName = x.RemoteDeviceName,
                    Path = files.StoreRootPath,
                    Files = files.Files.Select(y => new FileInfo
                    {
                        FileName = y.Name,
                        Path = y.StorePath,
                    }).ToList(),
                };
            }

            Debug.WriteLine($"Invalid data '{x.Data.ToString()}'.");
            throw new Exception($"Invalid data '{x.Data.ToString()}'.");
        }
    }
}
