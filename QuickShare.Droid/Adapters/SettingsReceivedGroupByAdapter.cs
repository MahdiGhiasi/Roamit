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
using QuickShare.Common.Classes;

namespace QuickShare.Droid.Adapters
{
    class SettingsReceivedGroupByAdapter : BaseAdapter<DownloadGroupByItem>, IListAdapter
    {
        public override DownloadGroupByItem this[int position] => DownloadGroupByItem.GroupItems[position];
        public override int Count => DownloadGroupByItem.GroupItems.Count;

        private Activity activity;

        public int SelectedItemPosition
        {
            get
            {
                var selectedState = new Classes.Settings(activity).DownloadGroupByState;
                for (int i = 0; i < DownloadGroupByItem.GroupItems.Count; i++)
                    if (DownloadGroupByItem.GroupItems[i].State == selectedState)
                        return i;
                return 0;
            }
        }


        public SettingsReceivedGroupByAdapter(Activity activity)
        {
            this.activity = activity;
        }
        
        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView; // re-use an existing view, if one is available
            if (view == null) // otherwise create a new one
                view = activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = this[position].ToString();
            return view;
        }
    }
}