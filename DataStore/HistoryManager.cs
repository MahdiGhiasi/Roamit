using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.DataStore
{
    public class HistoryManager : StorageManager<HistoryRow>
    {
        internal HistoryManager(string _dbPath) : base(_dbPath, "History")
        {
        }

        public bool ContainsKey(Guid guid)
        {
            return data.Exists(x => x.Id == guid);
        }

        public void Add(Guid guid, DateTime receiveTime, string senderName, IReceivedData receivedData, bool completed)
        {
            if (ContainsKey(guid))
                Remove(guid);

            HistoryRow r = new HistoryRow()
            {
                Id = guid,
                ReceiveTime = receiveTime,
                RemoteDeviceName = senderName,
                Data = receivedData,
                Completed = completed,
            };
            data.Insert(r);
        }

        public void Remove(Guid guid)
        {
            data.Delete(x => x.Id == guid);
        }

        public HistoryRow GetItem(Guid guid)
        {
            return data.FindById(guid);
        }

        public void ChangeCompletedStatus(Guid guid, bool isCompleted)
        {
            var item = GetItem(guid);
            item.Completed = isCompleted;
            data.Update(guid, item);
        }
    }
}
