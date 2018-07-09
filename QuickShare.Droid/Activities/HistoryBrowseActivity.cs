using System;
using System.Collections.Generic;
using System.IO;
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
using QuickShare.Droid.Classes;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace QuickShare.Droid.Activities
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.historybrowsepage")]
    internal class HistoryBrowseActivity : ThemeAwareActivity
    {
        private RecyclerView recyclerView;
        private HistoryBrowseAdapter dataAdapter;
        private LinearLayoutManager layoutManager;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.HistoryBrowse);

            var guid = Guid.Parse(Intent.GetStringExtra("guid"));
            HistoryRow history = await LoadHistoryRow(guid);
            InitPage(history);
        }

        private async Task<HistoryRow> LoadHistoryRow(Guid guid)
        {
            await DataStorageProviders.HistoryManager.OpenAsync();
            var history = DataStorageProviders.HistoryManager.GetItem(guid);
            DataStorageProviders.HistoryManager.Close();
            return history;
        }

        private void InitPage(HistoryRow history)
        {
            dataAdapter = new HistoryBrowseAdapter(history.Data as ReceivedFileCollection);

            recyclerView = FindViewById<RecyclerView>(Resource.Id.historyBrowse_RecyclerView);
            recyclerView.SetAdapter(dataAdapter);

            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);

            var toolbar = FindViewById<Toolbar>(Resource.Id.historyBrowse_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = $"Browse files";

            dataAdapter.ShareFileRequested += DataAdapter_ShareFileRequested;
            dataAdapter.OpenFileRequested += DataAdapter_OpenFileRequested;
            dataAdapter.FolderExpanded += DataAdapter_FolderExpanded;
            dataAdapter.GoneBack += DataAdapter_GoneBack;            
        }

        private void DataAdapter_ShareFileRequested(object sender, ReceivedFile e)
        {
            ShareHelper.ShareFile(this, new Java.IO.File(Path.Combine(e.StorePath, e.Name)));
        }

        private void DataAdapter_OpenFileRequested(object sender, ReceivedFile e)
        {
            LaunchHelper.OpenFile(this, Path.Combine(e.StorePath, e.Name));
        }

        private void DataAdapter_FolderExpanded(object sender, string e)
        {
            layoutManager.ScrollToPosition(0);
        }

        private void DataAdapter_GoneBack(object sender, EventArgs e)
        {
            layoutManager.ScrollToPosition(0);
        }

        public override void OnBackPressed()
        {
            if (dataAdapter.IsInRoot)
                base.OnBackPressed();
            else
                dataAdapter.GoBack();
        }
    }
}