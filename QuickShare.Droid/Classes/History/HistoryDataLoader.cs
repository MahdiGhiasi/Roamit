using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QuickShare.DataStore;

namespace QuickShare.Droid.Classes.History
{
    class HistoryDataLoader
    {
        private int pageSize;
        private List<HistoryRow> historyData = new List<HistoryRow>();

        public int ItemsCount { get; private set; }

        public HistoryDataLoader(int pageSize)
        {
            this.pageSize = pageSize;

            DataStorageProviders.HistoryManager.OpenAsync().Wait();
            this.ItemsCount = DataStorageProviders.HistoryManager.GetCount();
            historyData.AddRange(DataStorageProviders.HistoryManager.GetPage(0, pageSize).ToList());
            DataStorageProviders.HistoryManager.Close();
        }

        private void LoadNextPage()
        {
            historyData.AddRange(DataStorageProviders.HistoryManager.GetPage(historyData.Count, pageSize).ToList());
        }

        private async Task FetchUntil(int index)
        {
            if (index >= historyData.Count)
            {
                await DataStorageProviders.HistoryManager.OpenAsync();
                while (index >= historyData.Count)
                    LoadNextPage();
                DataStorageProviders.HistoryManager.Close();
            }
        }

        private async Task Prefetch(int index)
        {
            await Task.Run(async () =>
            {
                int nextIndex = Math.Min(index + pageSize, ItemsCount - 1);
                await FetchUntil(nextIndex);
            });
        }

        public async Task<HistoryRow> GetItem(int index)
        {
            if (index >= historyData.Count)
                await FetchUntil(index);
            else
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Prefetch(index);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return historyData[index];
        }

        internal void RemoveItem(int position)
        {
            historyData.RemoveAt(position);
            ItemsCount--;
        }

        internal void RefreshItem(int position)
        {
            var guid = historyData[position].Id;

            DataStorageProviders.HistoryManager.OpenAsync().Wait();
            historyData[position] = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();
        }

        public async Task<int> GetItemWithIdPosition(Guid guid)
        {
            int counter = 0;

            while (counter < ItemsCount)
            {
                if ((await GetItem(counter)).Id == guid)
                    return counter;

                counter++;
            }
            return -1;
        }
    }
}