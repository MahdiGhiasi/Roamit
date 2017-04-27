using QuickShare.Common.Rome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.TextTransfer
{
    public class TextSender
    {
        IRomePackageManager packageManager;
        int partLength = 1000;

        string deviceName = "remote device";

        public delegate void TextSendProgressEventHandler(TextSendEventArgs e);
        public event TextSendProgressEventHandler TextSendProgress;

        public TextSender(IRomePackageManager _packageManager, string _deviceName)
        {
            packageManager = _packageManager;
            deviceName = _deviceName;
        }

        public async Task<bool> Send(string text, ContentType contentType)
        {
            List<string> parts = Enumerable.Range(0, (int)Math.Ceiling(text.Length / ((double)partLength)))
                                           .Select(i => new string(text.Skip(i * (int)partLength)
                                                                       .Take((int)partLength)
                                                                       .ToArray())).ToList();

            //TODO: Add unique random id for each request and receive to file. (?)

            var requestGuid = Guid.NewGuid().ToString();

            for (int i = 0; i < parts.Count; i++)
            {
                Dictionary<string, object> vs = new Dictionary<string, object>
                {
                    { "Receiver", "TextReceiver" },
                    { "Type", (int)contentType },
                    { "PartNumber", i },
                    { "TotalParts", parts.Count },
                    { "Content", parts[i] },
                    { "UniqueId", requestGuid },
                    { "SenderName" , deviceName },
                };
                var result = await packageManager.Send(vs);

                if (result.Status != RomeAppServiceResponseStatus.Success)
                {
                    //TODO: Retry.
                    System.Diagnostics.Debug.WriteLine("TextSender.Send: Send failed (" + result.Status.ToString() + ")");
                    return false;
                }

                TextSendProgress?.Invoke(new TextSendEventArgs { SentParts = i + 1, TotalParts = parts.Count });
            }

            return true;
        }
    }
}
