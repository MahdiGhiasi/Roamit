using Microsoft.Win32;
using QuickShare.Desktop.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    public static class TaskbarThemeHelper
    {
        public static Theme GetTaskbarTheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
            {
                var value = key.GetValue("SystemUsesLightTheme");

                if (value == null)
                    return Theme.Dark;

                if (value.ToString() == "1")
                    return Theme.Light;

                return Theme.Dark;
            }
        }
    }
}
