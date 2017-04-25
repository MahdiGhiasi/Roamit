using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.TextTransfer
{
    internal static class ReceiveContentManager
    {
        static Dictionary<Guid, string> clipboardContents = new Dictionary<Guid, string>();

        internal static bool ContainsKey(Guid guid)
        {
            return clipboardContents.ContainsKey(guid);
        }

        internal static void Add(Guid guid, string v)
        {
            if (ContainsKey(guid))
            {
                clipboardContents[guid] = v;
            }
            else
            {
                clipboardContents.Add(guid, v);
            }
        }

        internal static void Remove(Guid guid)
        {
            clipboardContents.Remove(guid);
        }

        internal static string GetItem(Guid guid)
        {
            return clipboardContents[guid];
        }
    }
}
