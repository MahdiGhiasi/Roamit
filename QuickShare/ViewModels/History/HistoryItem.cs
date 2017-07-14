using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.ViewModels.History
{
    public abstract class HistoryItem
    {
        public DateTime ItemDateAndTime { get; set; }
        public string ItemDateAndTimeString { get => ItemDateAndTime.ToString(); }

        public Guid Guid { get; set; }

        private string senderName;
        public string SenderName
        {
            get { return $"from {senderName}"; }
            set { senderName = value; }
        }

    }
}
