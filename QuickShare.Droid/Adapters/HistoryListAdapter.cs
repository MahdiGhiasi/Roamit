using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using QuickShare.DataStore;
using QuickShare.Droid.Classes.History;

namespace QuickShare.Droid.Adapters
{
    internal class HistoryListAdapter : RecyclerView.Adapter
    {
        List<HistoryRow> historyData;

        public override int ItemCount => historyData.Count;
        public event EventHandler<HistoryRow> ItemClick;

        public HistoryListAdapter()
        {
            DataStorageProviders.HistoryManager.OpenAsync().Wait();
            historyData = DataStorageProviders.HistoryManager.GetAll().ToList();
            DataStorageProviders.HistoryManager.Close();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as HistoryItemHolder;
            var row = historyData[position];

            if (row.Data is ReceivedUrl)
            {
                vh.Title.Text = $"Link from {row.RemoteDeviceName}";
                vh.Subtitle.Text = Concat($"{(row.Data as ReceivedUrl).Uri.AbsolutePath}");
            }
            else if (row.Data is ReceivedText)
            {
                vh.Title.Text = $"Clipboard content from {row.RemoteDeviceName}";
                DataStorageProviders.TextReceiveContentManager.OpenAsync().Wait();
                var clipboardData = DataStorageProviders.TextReceiveContentManager.GetItemContent(row.Id);
                DataStorageProviders.TextReceiveContentManager.Close();
                vh.Subtitle.Text = Concat($"{clipboardData}");
            }
            else if (row.Data is ReceivedFile)
            {
                vh.Title.Text = $"File from {row.RemoteDeviceName}";
                vh.Subtitle.Text = Concat($"{System.IO.Path.GetFileName((row.Data as ReceivedFile).Name)}");
            }
            else if (row.Data is ReceivedFileCollection)
            {
                var files = row.Data as ReceivedFileCollection;
                if (files.Files.Count == 1)
                    vh.Title.Text = $"File from {row.RemoteDeviceName}";
                else
                    vh.Title.Text = $"{files.Files.Count} files from {row.RemoteDeviceName}";
                vh.Subtitle.Text = Concat($"{string.Join(", ", (row.Data as ReceivedFileCollection).Files.Select(x => System.IO.Path.GetFileName(x.Name)))}");
            }

            vh.Date.Text = $"{row.ReceiveTime}";
        }

        private string Concat(string s)
        {
            if (s.Length < 100)
                return s;
            else
                return s.Substring(0, 99) + "...";
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                               Inflate(Resource.Layout.HistoryListItemLayout, parent, false);
            HistoryItemHolder holder = new HistoryItemHolder(itemView, OnClick);
            return holder;
        }

        private void OnClick(int pos)
        {
            ItemClick?.Invoke(this, historyData[pos]);
        }
    }
}