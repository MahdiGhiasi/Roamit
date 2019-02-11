using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickShare.Classes
{
    public static class CommunicationMethodPreference
    {
        public static WindowsCommunicationMethodPreference WindowsCommunicationMethodPreference
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("WindowsCommunicationMethod"))
                {
                    var commMethod = ApplicationData.Current.LocalSettings.Values["WindowsCommunicationMethod"] as int?;
                    if (commMethod != null)
                        return (WindowsCommunicationMethodPreference)commMethod;
                }
                else if (ApplicationData.Current.LocalSettings.Values.ContainsKey("PreferProximityLocalConnection"))
                {
                    var preferProximityLocal = (ApplicationData.Current.LocalSettings.Values["PreferProximityLocalConnection"] as bool?) ?? false;

                    if (preferProximityLocal)
                        return WindowsCommunicationMethodPreference.NativeWhenInProximity;
                }

                return WindowsCommunicationMethodPreference.Cloud;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["WindowsCommunicationMethod"] = (int)value;
            }
        }

    }

    public enum WindowsCommunicationMethodPreference
    {
        Cloud = 0,
        NativeWhenInProximity = 1,
        Native =2,
    }
}
