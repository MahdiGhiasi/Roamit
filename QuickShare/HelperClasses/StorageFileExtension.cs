using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    public static class StorageFileExtension
    {
        public static bool IsLocallyAvailable(this StorageFile file)
        {
            return file.IsAvailable && ((file.Attributes & FileAttributes.LocallyIncomplete) == 0);
        }
    }
}
