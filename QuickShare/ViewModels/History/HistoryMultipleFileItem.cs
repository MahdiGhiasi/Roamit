using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace QuickShare.ViewModels.History
{
    public class HistoryMultipleFileItem : HistoryItem
    {
        public List<FileInfo> Files { get; set; }
        public string Path { get; set; }

        public string FilesCountString
        {
            get
            {
                if (Files.Count == 1)
                    return "File";
                else
                    return $"{Files.Count} files";
            }
        }

        public Visibility OpenContainingFolderVisibility
        {
            get
            {
                if ((Files != null) && (Files.Count == 1))
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }
        }
    }
}
