using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.DataStore
{
    public class HistoryRow
    {
        [BsonIndex(true)]
        public Guid RequestGuid { get; internal set; }

        public DateTime ReceiveTime { get; internal set; }
        public IReceivedData Data { get; internal set; }
        public string RemoteDeviceName { get; internal set; }

        public bool Completed { get; set; }
    }
}
