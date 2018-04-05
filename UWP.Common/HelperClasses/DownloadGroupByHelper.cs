using QuickShare.Common.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.HelperClasses
{
    public static class DownloadGroupByHelper
    {
        private static string settingKey = "DownloadGroupBy";

        public static DownloadGroupByItem GetState()
        {
            int state = (int)DownloadGroupByState.Month1; //Default value
            var localSettings = ApplicationData.Current.LocalSettings.Values;
            if (localSettings.ContainsKey(settingKey))
                int.TryParse(localSettings[settingKey].ToString(), out state);

            return DownloadGroupByItem.GroupItems.FirstOrDefault(x => x.State == (DownloadGroupByState)state);
        }

        public static void SetState(DownloadGroupByItem item)
        {
            ApplicationData.Current.LocalSettings.Values[settingKey] = ((int)(item.State)).ToString();
        }
    }
}
