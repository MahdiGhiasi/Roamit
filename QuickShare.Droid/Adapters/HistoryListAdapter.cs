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
        HistoryDataLoader historyDataLoader;
        Dictionary<int, HistoryItemHolder> holders = new Dictionary<int, HistoryItemHolder>();

        public override int ItemCount => historyDataLoader.ItemsCount;
        public event EventHandler<HistoryListItem> UrlLaunchRequested;
        public event EventHandler<HistoryListItem> CopyToClipboardRequested;
        public event EventHandler<HistoryListItem> BrowseFilesRequested;
        public event EventHandler<HistoryListItem> OpenFileRequested;
        public event EventHandler<HistoryListItem> MoveFilesRequested;
        public event EventHandler<HistoryListItem> RemoveItemRequested;
        public event EventHandler<HistoryListItem> ShareItemRequested;

        public HistoryListAdapter()
        {
            historyDataLoader = new HistoryDataLoader(50);
        }

        public HistoryItemHolder GetHolder(int position)
        {
            return holders[position];
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as HistoryItemHolder;
            var row = historyDataLoader.GetItem(position).GetAwaiter().GetResult();
            vh.Fill(row);

            holders[position] = vh;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                               Inflate(Resource.Layout.HistoryListItemLayout, parent, false);
            HistoryItemHolder holder = new HistoryItemHolder(itemView, OnClick);
            return holder;
        }

        public new void NotifyItemRemoved(int position)
        {
            historyDataLoader.RemoveItem(position);

            base.NotifyItemRemoved(position);
        }
      

        public new void NotifyItemChanged(int position)
        {
            historyDataLoader.RefreshItem(position);

            base.NotifyItemChanged(position);
        }

        private async void OnClick(int pos, HistoryItemHolder.EventAction action)
        {
            var item = await historyDataLoader.GetItem(pos);

            switch (action)
            {
                case HistoryItemHolder.EventAction.LaunchUrl:
                    UrlLaunchRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.CopyToClipboard:
                    CopyToClipboardRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.BrowseFiles:
                    BrowseFilesRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.OpenFile:
                    OpenFileRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.MoveFiles:
                    MoveFilesRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.RemoveItem:
                    RemoveItemRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                case HistoryItemHolder.EventAction.Share:
                    ShareItemRequested?.Invoke(this, new HistoryListItem(item, pos));
                    break;
                default:
                    break;
            }
        }
    }
}