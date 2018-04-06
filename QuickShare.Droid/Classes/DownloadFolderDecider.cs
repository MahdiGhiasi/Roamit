using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using PCLStorage;
using QuickShare.Common.Classes;
using QuickShare.Common.Interfaces;

namespace QuickShare.Droid.Classes
{
    class DownloadFolderDecider : IDownloadFolderDecider
    {
        Context context;
        public DownloadFolderDecider(Context context)
        {
            this.context = context;
        }

        public async Task<IFolder> DecideAsync(string[] fileTypes)
        {
            Settings settings = new Settings(context);

            var groupState = DownloadGroupByItem.GroupItems.FirstOrDefault(x => x.State == settings.DownloadGroupByState);
            var subfolderName = groupState.Decider(DateTime.Now);

            if (subfolderName.Length == 0)
                return GetFolder(settings.DefaultDownloadFolder);
            else
                return GetFolder(Path.Combine(settings.DefaultDownloadFolder, subfolderName));
        }

        private IFolder GetFolder(string folder)
        {
            Directory.CreateDirectory(folder); 
            return new FileSystemFolder(folder);
        }
    }
}