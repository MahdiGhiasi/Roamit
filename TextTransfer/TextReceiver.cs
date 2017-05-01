using QuickShare.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.TextTransfer
{
    public static class TextReceiver
    {
        public delegate void TextReceiveFinishedEventHandler(TextReceiveEventArgs e);
        public static event TextReceiveFinishedEventHandler TextReceiveFinished;

        public static bool ReceiveRequest(Dictionary<string, object> data)
        {
            var type = (ContentType)data["Type"];
            Guid guid;
            if (!Guid.TryParse((string)data["UniqueId"], out guid))
            {
                TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                {
                    Success = false,
                    Guid = null,
                    HostName = "",
                });
                return false;
            }
            
            if (type == ContentType.ClipboardContent)
            {
                var partNumber = (int)data["PartNumber"];
                var totalParts = (int)data["TotalParts"];

                if (!DataStorageProviders.TextReceiveContentManager.IsOpened)
                    DataStorageProviders.TextReceiveContentManager.Open();

                if (partNumber == 0)
                    DataStorageProviders.TextReceiveContentManager.Add(guid, "");

                if (!DataStorageProviders.TextReceiveContentManager.ContainsKey(guid))
                {
                    TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                    {
                        Success = false,
                        Guid = guid,
                        HostName = (string)data["SenderName"],
                    });
                    return false;
                }

                DataStorageProviders.TextReceiveContentManager.Add(guid, DataStorageProviders.TextReceiveContentManager.GetItemContent(guid) + (string)data["Content"]);

                if (partNumber == (totalParts - 1)) //Finished receiving data.
                {
                    TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                    {
                        Success = true,
                        Guid = guid,
                        HostName = (string)data["SenderName"],
                    });

                    DataStorageProviders.HistoryManager.Open();
                    DataStorageProviders.HistoryManager.Add(guid, 
                        DateTime.Now,
                        (string)data["SenderName"],
                        new ReceivedText(),
                        true);
                    DataStorageProviders.HistoryManager.Close();
                }

                DataStorageProviders.TextReceiveContentManager.Close();
            }
            
            return true;
        }

        public static void ClearEventRegistrations()
        {
            TextReceiveFinished = null;
        }
    }
}
