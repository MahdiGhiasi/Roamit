using System;
using System.Collections.Generic;
using System.IO;
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
    internal class HistoryBrowseAdapter : RecyclerView.Adapter
    {
        private ReceivedFileCollection fileCollection;

        public event EventHandler<ReceivedFile> OpenFileRequested;
        public event EventHandler<ReceivedFile> ShareFileRequested;
        public event EventHandler<string> FolderExpanded;
        public event EventHandler GoneBack;

        private string currentPath;
        public string CurrentPath
        {
            get
            {
                return currentPath;
            }
            private set
            {
                currentPath = RemoveLastSlash(value);
                Update();
            }
        }

        public override int ItemCount => FilesCount + FoldersCount;
        public int FilesCount => FilesInCurrentPath.Count;
        public int FoldersCount => FoldersInCurrentPath.Count;
        public bool IsInRoot => RemoveLastSlash(fileCollection.StoreRootPath) == CurrentPath;

        public List<ReceivedFile> FilesInCurrentPath { get; private set; }
        public List<string> FoldersInCurrentPath { get; private set; }

        public HistoryBrowseAdapter(ReceivedFileCollection fileCollection)
        {
            this.fileCollection = fileCollection;
            CurrentPath = fileCollection.StoreRootPath;
        }

        private void Update()
        {
            FilesInCurrentPath = fileCollection.Files.Where(x => RemoveLastSlash(x.StorePath) == CurrentPath).OrderBy(x => x.Name).ToList();
            FoldersInCurrentPath = fileCollection.Files.Where(x => RemoveLastSlash(x.StorePath).Length > CurrentPath.Length && x.StorePath.Substring(0, CurrentPath.Length) == CurrentPath)
                    .Select(x => x.StorePath.Substring(CurrentPath.Length).Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).First()).Distinct().OrderBy(x => x).ToList();
            if (!IsInRoot)
                FoldersInCurrentPath.Insert(0, "..");
        }

        private object GetItem(int position)
        {
            try
            {
                if (position < FoldersCount)
                    return FoldersInCurrentPath[position];
                else
                    return FilesInCurrentPath[position - FoldersCount];
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as HistoryBrowseItemHolder;
            vh.Fill(GetItem(position)); 
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                               Inflate(Resource.Layout.HistoryBrowseItemLayout, parent, false);
            var holder = new HistoryBrowseItemHolder(itemView, OnClick);
            return holder;
        }

        private void OnClick(int pos, HistoryBrowseItemHolder.EventAction action)
        {
            switch (action)
            {
                case HistoryBrowseItemHolder.EventAction.OpenFile:
                    OpenFileRequested?.Invoke(this, GetItem(pos) as ReceivedFile);
                    break;
                case HistoryBrowseItemHolder.EventAction.ShareFile:
                    ShareFileRequested?.Invoke(this, GetItem(pos) as ReceivedFile);
                    break;
                case HistoryBrowseItemHolder.EventAction.ExpandFolder:
                    if (GetItem(pos) as string == "..")
                    {
                        GoBack();
                        GoneBack?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        ExpandFolder(GetItem(pos) as string);
                        FolderExpanded?.Invoke(this, GetItem(pos) as string);
                    }
                    break;
                default:
                    break;
            }
        }

        public void ExpandFolder(string folderName)
        {
            CurrentPath = CurrentPath + "/" + folderName;
            NotifyDataSetChanged();
        }

        public void GoBack()
        {
            if (IsInRoot)
                return;

            CurrentPath = CurrentPath.Remove(CurrentPath.LastIndexOfAny(new char[] { '/', '\\' }));
            NotifyDataSetChanged();
        }

        private string RemoveLastSlash(string path)
        {
            if (path.Length == 0)
                return path;
            return (path.Last() == '/' || path.Last() == '\\') ? path.Remove(path.Length - 1) : path;
        }
    }
}