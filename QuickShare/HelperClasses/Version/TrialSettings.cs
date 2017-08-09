using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.HelperClasses.Version
{
    static class TrialSettings
    {
        static TrialSettings()
        {
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("IsTrialCache"))
                isTrial = (Windows.Storage.ApplicationData.Current.LocalSettings.Values["IsTrialCache"] as bool?) ?? false;
        }

        private static bool isTrial = false;

        public static bool IsTrial
        {
            get
            {
                return isTrial;
            }
            set
            {
                isTrial = value;
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["IsTrialCache"] = value;
                IsTrialChanged?.Invoke();
            }
        }

        public delegate void IsTrialChangedEventHandler();
        public static event IsTrialChangedEventHandler IsTrialChanged;
    }
}
