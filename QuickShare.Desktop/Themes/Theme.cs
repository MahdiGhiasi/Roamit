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
        DarkOpaque,
        LightOpaque,
    }

    public static class ThemeSelector
    {
        public static Uri GetThemeUrl(Theme theme)
        {
            switch (theme)
            {
                case Theme.LightOpaque:
                    return new Uri("pack://application:,,,/Themes/LightOpaque.xaml");
                case Theme.DarkOpaque:
                    return new Uri("pack://application:,,,/Themes/DarkOpaque.xaml");
                case Theme.Light:
                    return new Uri("pack://application:,,,/Themes/Light.xaml");
                case Theme.Dark:
                default:
                    return new Uri("pack://application:,,,/Themes/Dark.xaml");
            }
        }
    }
}
