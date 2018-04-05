using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Interfaces
{
    public interface IDownloadFolderDecider
    {
        Task<IFolder> DecideAsync(string[] fileTypes);
    }
}
