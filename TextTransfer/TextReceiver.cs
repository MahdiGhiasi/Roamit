using Newtonsoft.Json;
using QuickShare.DataStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.TextTransfer
{
    public static class TextReceiver
    {
        public delegate void TextReceiveFinishedEventHandler(TextReceiveEventArgs e);
        public static event TextReceiveFinishedEventHandler TextReceiveFinished;

        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static async Task<bool> ReceiveRequest(Dictionary<string, object> data)
        {
            await semaphore.WaitAsync();

            try
            {
                var type = (ContentType)data["Type"];
                if (!Guid.TryParse((string)data["UniqueId"], out Guid guid))
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

                    if (!DataStorageProviders.TextReceiveContentManager.ContainsKey(guid))
                        DataStorageProviders.TextReceiveContentManager.Add(guid, JsonConvert.SerializeObject(new Dictionary<int, string>()));

                    Dictionary<int, string> parts = JsonConvert.DeserializeObject<Dictionary<int, string>>(DataStorageProviders.TextReceiveContentManager.GetItemContent(guid));

                    parts[partNumber] = (string)data["Content"];

                    DataStorageProviders.TextReceiveContentManager.Add(guid, JsonConvert.SerializeObject(parts));

                    if (parts.Count != totalParts)
                    {
                        DataStorageProviders.TextReceiveContentManager.Close();
                    }
                    else //Finished receiving data.
                    {
                        string finalString = "";
                        for (int i = 0; i < totalParts; i++)
                        {
                            finalString += parts[i];
                        }
                        DataStorageProviders.TextReceiveContentManager.Add(guid, finalString);
                        DataStorageProviders.TextReceiveContentManager.Close();

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
            catch
            {
                if (semaphore.CurrentCount == 0)
                    semaphore.Release();
                throw;
            }
            finally
            {
                if (semaphore.CurrentCount == 0)
                    semaphore.Release();
            }
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
