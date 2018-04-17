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
using QuickShare.DataStore;

namespace QuickShare.Droid.Classes.History
{
    public class HistoryListItem
    {
        public HistoryRow Data { get; }
        public int Position { get; }

        public HistoryListItem(HistoryRow data, int position)
        {
            Data = data;
            Position = position;
        }
    }
}