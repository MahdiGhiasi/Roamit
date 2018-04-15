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
using QuickShare.Droid.Adapters;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace QuickShare.Droid.Activities
{
    [Activity(Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.historylistpage")]
    internal class HistoryListActivity : ThemeAwareActivity
    {
        private RecyclerView mRecyclerView;
        private HistoryListAdapter mAdapter;
        private LinearLayoutManager mLayoutManager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.HistoryList);

            mAdapter = new HistoryListAdapter();
            mAdapter.ItemClick += MAdapter_ItemClick;

            mRecyclerView = FindViewById<RecyclerView>(Resource.Id.historyList_RecyclerView);
            mRecyclerView.SetAdapter(mAdapter);

            mLayoutManager = new LinearLayoutManager(this);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Receive History";
        }

        private void MAdapter_ItemClick(object sender, DataStore.HistoryRow e)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle($"{e.Id}");
            alert.SetPositiveButton("OK", (senderAlert, args) => { });
            RunOnUiThread(() => {
                alert.Show();
            });
        }
    }
}