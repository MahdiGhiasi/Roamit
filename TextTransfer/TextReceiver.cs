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
                    Content = "",
                });
                return false;
            }

            if (type == ContentType.ClipboardContent)
            {
                var partNumber = (int)data["PartNumber"];
                var totalParts = (int)data["TotalParts"];

                if (partNumber == 0)
                    ReceiveContentManager.Add(guid, "");

                if (!ReceiveContentManager.ContainsKey(guid))
                {
                    TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                    {
                        Success = false,
                        Content = "",
                    });
                    return false;
                }

                ReceiveContentManager.Add(guid, ReceiveContentManager.GetItem(guid) + (string)data["Content"]);

                if (partNumber == (totalParts - 1)) //Finished receiving data.
                {
                    TextReceiveFinished?.Invoke(new TextReceiveEventArgs
                    {
                        Success = true,
                        Content = ReceiveContentManager.GetItem(guid),
                    });

                    ReceiveContentManager.Remove(guid);
                }

            }

            return true;
        }
    }
}
