using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using QuickShare.DataStore;

namespace QuickShare.Droid.Classes.History
{
    public class HistoryItemHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; private set; }
        public TextView TextPreview { get; private set; }
        public TextView Date { get; private set; }
        public Button OpenUrl { get; private set; }
        public Button CopyToClipboard { get; private set; }
        public Button BrowseFiles { get; private set; }
        public Button OpenFile { get; private set; }
        public Button RemoveItem { get; private set; }

        public HistoryItemHolder(View itemView, Action<int, EventAction> listener) : 
            base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_title);
            TextPreview = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_textPreview);
            Date = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_date);
            OpenUrl = itemView.FindViewById<Button>(Resource.Id.historyListItemLayout_openUrl);
            CopyToClipboard = itemView.FindViewById<Button>(Resource.Id.historyListItemLayout_copyToClipboard);
            BrowseFiles = itemView.FindViewById<Button>(Resource.Id.historyListItemLayout_browseFiles);
            OpenFile = itemView.FindViewById<Button>(Resource.Id.historyListItemLayout_openFile);
            RemoveItem = itemView.FindViewById<Button>(Resource.Id.historyListItemLayout_removeItem);

            OpenUrl.Click += (sender, e) => listener(base.LayoutPosition, EventAction.LaunchUrl);
            CopyToClipboard.Click += (sender, e) => listener(base.LayoutPosition, EventAction.CopyToClipboard);
            BrowseFiles.Click += (sender, e) => listener(base.LayoutPosition, EventAction.BrowseFiles);
            OpenFile.Click += (sender, e) => listener(base.LayoutPosition, EventAction.OpenFile);
            RemoveItem.Click += (sender, e) => listener(base.LayoutPosition, EventAction.RemoveItem);
        }

        internal void Fill(HistoryRow row)
        {
            if (row.Data is ReceivedUrl)
            {
                this.Title.TextFormatted = GetCardTitle("Link", row.RemoteDeviceName);
                this.TextPreview.Text = Concat($"{(row.Data as ReceivedUrl).Uri.OriginalString}");

                SetButtonsVisibility(this, HistoryItemState.Url);
            }
            else if (row.Data is ReceivedText)
            {
                this.Title.TextFormatted = GetCardTitle("Clipboard content", row.RemoteDeviceName);
                DataStorageProviders.TextReceiveContentManager.OpenAsync().GetAwaiter().GetResult();
                if (DataStorageProviders.TextReceiveContentManager.ContainsKey(row.Id))
                {
                    this.TextPreview.Text = Concat(DataStorageProviders.TextReceiveContentManager.GetItemContent(row.Id));
                    SetButtonsVisibility(this, HistoryItemState.Text);
                }
                else
                {
                    this.TextPreview.Text = "Corrupted data.";
                    SetButtonsVisibility(this, HistoryItemState.None);
                }
                DataStorageProviders.TextReceiveContentManager.Close();

            }
            else if (row.Data is ReceivedFile)
            {
                this.Title.TextFormatted = GetCardTitle("File", row.RemoteDeviceName);
                this.TextPreview.Text = Concat($"{System.IO.Path.GetFileName((row.Data as ReceivedFile).Name)}");

                SetButtonsVisibility(this, HistoryItemState.SingleFile);
            }
            else if (row.Data is ReceivedFileCollection)
            {
                var files = row.Data as ReceivedFileCollection;
                if (files.Files.Count == 1)
                {
                    this.Title.TextFormatted = GetCardTitle("File", row.RemoteDeviceName);

                    SetButtonsVisibility(this, HistoryItemState.SingleFile);
                }
                else
                {
                    this.Title.TextFormatted = GetCardTitle($"{files.Files.Count} files", row.RemoteDeviceName);

                    SetButtonsVisibility(this, HistoryItemState.MultipleFiles);
                }

                this.TextPreview.Text = Concat(GetFileListPreview(row.Data as ReceivedFileCollection));
            }

            this.Date.Text = $"{row.ReceiveTime}";
        }


        private string GetFileListPreview(ReceivedFileCollection receivedFileCollection)
        {
            var files = receivedFileCollection.Files.Select(x => System.IO.Path.GetFileName(x.Name)).OrderBy(x => x).Select(x => Concat(x, 25)).ToList();
            var filesPart = (files.Count <= 3) ? files : files.Take(3).Concat(new string[] { $"and {files.Count - 3} other file{(files.Count - 3 > 1 ? "s" : "")}." });

            return $"{string.Join(", ", filesPart)}";
        }

        private SpannableString GetCardTitle(string type, string remoteDeviceName)
        {
            SpannableString str = new SpannableString($"{type} from {remoteDeviceName}");
            str.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, type.Length, SpanTypes.ExclusiveExclusive);
            return str;
        }

        private string Concat(string s, int maxLength = 100)
        {
            if (s.Length < maxLength)
                return s;
            else
                return s.Substring(0, maxLength - 1) + "...";
        }

        private void SetButtonsVisibility(HistoryItemHolder holder, HistoryItemState state)
        {
            switch (state)
            {
                case HistoryItemState.Url:
                    holder.OpenFile.Visibility = ViewStates.Gone;
                    holder.BrowseFiles.Visibility = ViewStates.Gone;
                    holder.OpenUrl.Visibility = ViewStates.Visible;
                    holder.CopyToClipboard.Visibility = ViewStates.Visible;
                    break;
                case HistoryItemState.Text:
                    holder.OpenFile.Visibility = ViewStates.Gone;
                    holder.BrowseFiles.Visibility = ViewStates.Gone;
                    holder.OpenUrl.Visibility = ViewStates.Gone;
                    holder.CopyToClipboard.Visibility = ViewStates.Visible;
                    break;
                case HistoryItemState.SingleFile:
                    holder.OpenFile.Visibility = ViewStates.Visible;
                    holder.BrowseFiles.Visibility = ViewStates.Gone;
                    holder.OpenUrl.Visibility = ViewStates.Gone;
                    holder.CopyToClipboard.Visibility = ViewStates.Gone;
                    break;
                case HistoryItemState.MultipleFiles:
                    holder.OpenFile.Visibility = ViewStates.Gone;
                    holder.BrowseFiles.Visibility = ViewStates.Visible;
                    holder.OpenUrl.Visibility = ViewStates.Gone;
                    holder.CopyToClipboard.Visibility = ViewStates.Gone;
                    break;
                default:
                    holder.OpenFile.Visibility = ViewStates.Gone;
                    holder.BrowseFiles.Visibility = ViewStates.Gone;
                    holder.OpenUrl.Visibility = ViewStates.Gone;
                    holder.CopyToClipboard.Visibility = ViewStates.Gone;
                    break;
            }
        }

        private enum HistoryItemState
        {
            None,
            Url,
            Text,
            SingleFile,
            MultipleFiles,
        }

        public enum EventAction
        {
            LaunchUrl,
            CopyToClipboard,
            BrowseFiles,
            OpenFile,
            RemoveItem,
        }
    }
}