using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.ConnectedDevices;

namespace QuickShare.Droid
{
    class DevicesListAdapter : BaseAdapter<string>, IListAdapter
    {
        Activity context;
        DevicesListManager.DevicesListManager listManager;

        public override string this[int position]
        {
            get { return listManager.RemoteSystems[position].DisplayName; }
        }

        public override int Count
        {
            get { return listManager.RemoteSystems.Count; }
        }

        public DevicesListAdapter(Activity _context, DevicesListManager.DevicesListManager _listManager) : base()
        {
            context = _context;
            listManager = _listManager;

            listManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;
        }

        private void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            context.RunOnUiThread(() =>
            {
                this.NotifyDataSetChanged();
            });
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
                view = context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = this[position];
            return view;
        }
    }
}