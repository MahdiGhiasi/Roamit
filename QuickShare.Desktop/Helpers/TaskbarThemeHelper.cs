using Microsoft.Win32;
using QuickShare.Desktop.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace QuickShare.Desktop.Helpers
{
    public static class TaskbarThemeHelper
    {
        [DllImport("uxtheme.dll", EntryPoint = "#95", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        public static extern UInt32 GetImmersiveColorFromColorSetEx(UInt32 immersiveColorSet, UInt32 immersiveColorType,
            Boolean ignoreHighContrast, UInt32 highContrastCacheMode);

        [DllImport("uxtheme.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "#96")]
        public static extern UInt32 GetImmersiveColorTypeFromName(IntPtr pName);

        [DllImport("Uxtheme.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "#98")]
        public static extern UInt32 GetImmersiveUserColorSetPreference(Boolean bForceCheckRegistry, Boolean bSkipCheckOnFail);

        public static Color GetAccentColor()
        {
            uint colorSystemAccent = GetImmersiveColorFromColorSetEx(GetImmersiveUserColorSetPreference(false, false),
                GetImmersiveColorTypeFromName(Marshal.StringToHGlobalUni("ImmersiveSystemAccent")), false, 0);

            var color = Color.FromArgb((byte)((0xFF000000 & colorSystemAccent) >> 24), 
                (byte)(0xFF & colorSystemAccent), 
                (byte)((0xFF00 & colorSystemAccent) >> 8), 
                (byte)((0xFF0000 & colorSystemAccent) >> 16));

            return color;
        }


        public static Theme GetTaskbarTheme()
        {
            switch (GetTaskbarThemeInternal())
            {
                case Theme.Dark:
                    if (IsTaskbarTransparencyEnabled())
                        return Theme.Dark;
                    else
                        return Theme.DarkOpaque;
                case Theme.Light:
                    if (IsTaskbarTransparencyEnabled())
                        return Theme.Light;
                    else
                        return Theme.LightOpaque;
                default:
                    return Theme.Dark;
            }
        }

        private static Theme GetTaskbarThemeInternal()
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

        public static bool IsTaskbarColored()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
            {
                var value = key.GetValue("ColorPrevalence");

                if (value == null)
                    return false;

                if (value.ToString() == "1")
                    return true;

                return false;
            }
        }

        public static bool IsTaskbarTransparencyEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
            {
                var value = key.GetValue("EnableTransparency");

                if (value == null)
                    return true;

                if (value.ToString() == "1")
                    return true;

                return false;
            }
        }
    }
}
