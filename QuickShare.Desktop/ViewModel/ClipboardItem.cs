using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.ViewModel
{
    internal class ClipboardItem
    {
        public string DisplayText { get; private set; }

        string text;
        public string Text
        {
            get
            {
                return text;
            }
            private set
            {
                text = value;
                DisplayText = NormalizeForDisplay(value);
            }
        }

        public DateTime CopyDate { get; private set; }

        public string CopyDateString
        {
            get
            {
                return CopyDate.ToShortTimeString();
            }
        }

        public ClipboardItem(string _text)
        {
            Text = _text;
            CopyDate = DateTime.Now;
        }

        private string NormalizeForDisplay(string value)
        {
            value = value.Replace('\t', ' ').Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

            while (value.Contains("  "))
                value = value.Replace("  ", " ");

            if (value.Length < 100)
                return value;
            else
                return value.Substring(0, 95) + "...";
        }
    }
}
