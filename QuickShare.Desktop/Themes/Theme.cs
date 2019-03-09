using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Themes
{
    public enum Theme
    {
        Dark,
        Light,
    }

    public static class ThemeSelector
    {
        public static Uri GetThemeUrl(Theme theme)
        {
            switch (theme)
            {
                case Theme.Light:
                    return new Uri("/Themes/Light.xaml");
                case Theme.Dark:
                default:
                    return new Uri("/Themes/Dark.xaml");
            }
        }
    }
}
