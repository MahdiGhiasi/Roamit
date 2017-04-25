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

        public TextSender(IRomePackageManager _packageManager)
        {
            packageManager = _packageManager;
        }

        public async Task<bool> Send(string text, ContentType contentType)
        {
            List<string> parts = new List<string>();

            while (text.Length > 0)
            {
                parts.Add(text.Substring(0, partLength));
                text = text.Substring(partLength); //TODO: fix this.
            }

            //TODO: Add unique random id for each request and receive to file. (?)

            for (int i = 0; i < parts.Count; i++)
            {
                Dictionary<string, object> vs = new Dictionary<string, object>
                {
                    { "Receiver", "TextReceiver" },
                    { "Type", contentType },
                    { "PartNumber", i },
                    { "TotalParts", parts.Count },
                    { "Content", parts[i] },
                    { "UniqueId", Guid.NewGuid().ToString() },
                };
                var result = await packageManager.Send(vs);

                if (result.Status != RomeAppServiceResponseStatus.Success)
                {
                    //TODO: Retry.
                    System.Diagnostics.Debug.WriteLine("TextSender.Send: Send failed (" + result.Status.ToString() + ")");
                    return false;
                }
            }

            return true;
        }
    }
}
