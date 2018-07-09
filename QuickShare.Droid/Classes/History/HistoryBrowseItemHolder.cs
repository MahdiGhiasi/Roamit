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

namespace QuickShare.Droid.Classes.History
{
    public class HistoryBrowseItemHolder : RecyclerView.ViewHolder
    {
        public TextView Name { get; private set; }
        public TextView FolderIcon { get; private set; }
        public Button OpenFile { get; private set; }
        public Button ShareFile { get; private set; }
        public LinearLayout Item { get; private set; }

        public bool IsFolder
        {
            get
            {
                return FolderIcon.Visibility == ViewStates.Visible;
            }
            private set
            {
                FolderIcon.Visibility = value ? ViewStates.Visible : ViewStates.Gone;
            }
        }

        public HistoryBrowseItemHolder(View itemView, Action<int, EventAction> listener) :
            base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.historyBrowseItemLayout_itemName);
            FolderIcon = itemView.FindViewById<TextView>(Resource.Id.historyBrowseItemLayout_folderIcon);
            OpenFile = itemView.FindViewById<Button>(Resource.Id.historyBrowseItemLayout_openFileButton);
            ShareFile = itemView.FindViewById<Button>(Resource.Id.historyBrowseItemLayout_shareFileButton);
            Item = itemView.FindViewById<LinearLayout>(Resource.Id.historyBrowseItemLayout_item);

            OpenFile.Click += (sender, e) => listener(base.LayoutPosition, EventAction.OpenFile);
            ShareFile.Click += (sender, e) => listener(base.LayoutPosition, EventAction.ShareFile);
            Item.Click += (sender, e) => listener(base.LayoutPosition, IsFolder ? EventAction.ExpandFolder : EventAction.OpenFile);
        }

        internal void Fill(object item)
        {
            if (item is string)
                FillFolder(item as string);
            else
                FillFile(item as ReceivedFile);
        }

        private void FillFile(ReceivedFile receivedFile)
        {
            Name.Text = receivedFile.Name;
            OpenFile.Visibility = ViewStates.Visible;
            ShareFile.Visibility = ViewStates.Visible;
            IsFolder = false;
        }

        private void FillFolder(string folderName)
        {
            Name.Text = folderName;
            OpenFile.Visibility = ViewStates.Gone;
            ShareFile.Visibility = ViewStates.Gone;
            IsFolder = true;
        }

        public enum EventAction
        {
            OpenFile,
            ShareFile,
            ExpandFolder,
        }
    }
}
