using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using QuickShare.DataStore;
using QuickShare.Droid.Adapters;
using QuickShare.Droid.Classes.History;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace QuickShare.Droid.Activities
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.historylistpage")]
    internal class HistoryListActivity : ThemeAwareActivity
    {
        private RecyclerView historyRecyclerView;
        private HistoryListAdapter historyAdapter;
        private LinearLayoutManager historyLayoutManager;
        private TextView historyEmptyMessage;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.HistoryList);

            historyAdapter = new HistoryListAdapter();
            historyAdapter.BrowseFilesRequested += HistoryAdapter_BrowseFilesRequested;
            historyAdapter.CopyToClipboardRequested += HistoryAdapter_CopyToClipboardRequested;
            historyAdapter.OpenFileRequested += HistoryAdapter_OpenFileRequested;
            historyAdapter.RemoveItemRequested += HistoryAdapter_RemoveItemRequested;
            historyAdapter.UrlLaunchRequested += HistoryAdapter_UrlLaunchRequested;

            historyRecyclerView = FindViewById<RecyclerView>(Resource.Id.historyList_RecyclerView);
            historyRecyclerView.SetAdapter(historyAdapter);

            historyEmptyMessage = FindViewById<TextView>(Resource.Id.historyList_emptyMessage);
            historyEmptyMessage.Visibility = (historyAdapter.ItemCount == 0) ? ViewStates.Visible : ViewStates.Gone;

            historyLayoutManager = new LinearLayoutManager(this);
            historyRecyclerView.SetLayoutManager(historyLayoutManager);

            var toolbar = FindViewById<Toolbar>(Resource.Id.historyList_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Receive History";

            
        }

        private void HistoryAdapter_BrowseFilesRequested(object sender, HistoryListItem e)
        {
            throw new NotImplementedException();
        }

        private void HistoryAdapter_CopyToClipboardRequested(object sender, HistoryListItem e)
        {
            throw new NotImplementedException();
        }

        private void HistoryAdapter_OpenFileRequested(object sender, HistoryListItem e)
        {
            throw new NotImplementedException();
        }

        private async void HistoryAdapter_RemoveItemRequested(object sender, HistoryListItem e)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Remove(e.Data.Id);
            DataStorageProviders.HistoryManager.Close();

            historyAdapter.NotifyItemRemoved(e.Position);
        }

        private void HistoryAdapter_UrlLaunchRequested(object sender, HistoryListItem e)
        {
            throw new NotImplementedException();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.history, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_history_clearHistory:
                    Android.App.AlertDialog.Builder builder = new Android.App.AlertDialog.Builder(this);
                    builder.SetMessage("Are you sure you want to clear receive history?")
                        .SetPositiveButton("Yes", ClearHistoryDialogClickListener)
                        .SetNegativeButton("No", (IDialogInterfaceOnClickListener)null)
                        .Show();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private async void ClearHistoryDialogClickListener(object sender, DialogClickEventArgs e)
        {
            await ClearHistory();
        }

        private async Task ClearHistory()
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Clear();
            DataStorageProviders.HistoryManager.Close();

            Finish();
            StartActivity(Intent);
            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
        }
    }
}