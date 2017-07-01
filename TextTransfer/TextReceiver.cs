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

        public static async Task<bool> ReceiveRequest(Dictionary<string, object> data)
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
                    await DataStorageProviders.TextReceiveContentManager.OpenAsync();

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
                DataStorageProviders.TextReceiveContentManager.Close();

                if (partNumber == (totalParts - 1)) //Finished receiving data.
                {
                    await DataStorageProviders.HistoryManager.OpenAsync();
                    DataStorageProviders.HistoryManager.Add(guid, 
                        DateTime.Now,
                        (string)data["SenderName"],
                        new ReceivedText(),
                        true);
                    DataStorageProviders.HistoryManager.Close();

                    TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                    {
                        Success = true,
                        Guid = guid,
                        HostName = (string)data["SenderName"],
                    });
                }
            }
            
            return true;
        }

        public static async Task<Guid> QuickTextReceivedAsync(string sender, string text)
        {
            Guid guid = Guid.NewGuid();

            await DataStorageProviders.TextReceiveContentManager.OpenAsync();
            DataStorageProviders.TextReceiveContentManager.Add(guid, text);
            DataStorageProviders.TextReceiveContentManager.Close();

            await DataStorageProviders.HistoryManager.OpenAsync();
            DataStorageProviders.HistoryManager.Add(guid,
                DateTime.Now,
                sender,
                new ReceivedText(),
                true);
            DataStorageProviders.HistoryManager.Close();

            return guid;
        }

        public static void ClearEventRegistrations()
        {
            TextReceiveFinished = null;
        }
    }
}
