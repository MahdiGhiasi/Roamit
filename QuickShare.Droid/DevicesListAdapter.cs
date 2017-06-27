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
using Java.Lang;
using Microsoft.ConnectedDevices;
using QuickShare.DevicesListManager;

namespace QuickShare.Droid
{
    class DevicesListAdapter : BaseAdapter<string>, IListAdapter
    {
        Activity context;
        DevicesListManager.DevicesListManager listManager;
        List<NormalizedRemoteSystem> systems = new List<NormalizedRemoteSystem>();

        public override string this[int position]
        {
            get
            {
                try
                {
                    string s;
                    lock (systems)
                    {
                        s = systems[position].DisplayName;
                    }
                    return s;
                }
                catch
                {
                    return null;
                }
            }
        }

        public override int Count
        {
            get { return systems.Count; }
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
                lock (systems)
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                    {
                        systems.Clear();
                        this.NotifyDataSetChanged();
                    }

                    if (e.NewItems != null)
                        foreach (var item in e.NewItems)
                        {
                            if (item is NormalizedRemoteSystem nItem)
                            {
                                if (systems.FirstOrDefault(x => x.Id == nItem.Id) == null)
                                {
                                    systems.Add(item as NormalizedRemoteSystem);
                                    this.NotifyDataSetChanged();
                                }
                            }
                        }

                    if (e.OldItems != null)
                        foreach (var item in e.OldItems)
                            if (item is NormalizedRemoteSystem nItem)
                            {
                                var sysItem = systems.FirstOrDefault(x => x.Id == nItem.Id);
                                if (sysItem != null)
                                {
                                    systems.Remove(sysItem);
                                    this.NotifyDataSetChanged();
                                }
                            }
                }
            });
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public NormalizedRemoteSystem GetItemFromId(long id)
        {
            return systems[(int)id];
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