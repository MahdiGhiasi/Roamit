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

namespace QuickShare.Droid.Classes.History
{
    public class HistoryItemHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; private set; }
        public TextView Subtitle { get; private set; }
        public TextView Date { get; private set; }

        public HistoryItemHolder(View itemView, Action<int> listener) : 
            base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_title);
            Subtitle = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_subtitle);
            Date = itemView.FindViewById<TextView>(Resource.Id.historyListItemLayout_date);

            itemView.Click += (sender, e) => listener(base.LayoutPosition);
        }
    }
}