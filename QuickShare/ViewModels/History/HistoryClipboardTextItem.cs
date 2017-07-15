using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.ViewModels.History
{
    public class HistoryClipboardTextItem : HistoryItem
    {
        private string content;
        public string Content
        {
            get { return content; }
            set
            {
                content = value;

                if (content.Length > 200)
                    contentPreview = content.Substring(0, 195) + "...";
                else
                    contentPreview = content;
            }
        }

        private string contentPreview;
        public string ContentPreview
        {
            get { return contentPreview; }
            private set { contentPreview = value; }
        }


    }
}
