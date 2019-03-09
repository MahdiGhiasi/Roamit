using Microsoft.Shell;
using QuickShare.Desktop.Themes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QuickShare.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application , ISingleInstanceApp
    {
#if SQUIRREL
        private const string Unique = "RoamitDesktopExtension";
#else
        private const string Unique = "RoamitDesktopExtension2";
#endif

        public ResourceDictionary ThemeDictionary
        {
            get { return Resources.MergedDictionaries[0]; }
        }

        // Single instance code from http://blogs.microsoft.co.il/arik/2010/05/28/wpf-single-instance-application/
        [STAThread]
        public static void Main()
        {
#if SQUIRREL
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();

                application.InitializeComponent();
                application.ChangeTheme(Theme.Dark);
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
#else
            var application = new App();

            application.InitializeComponent();
            application.ChangeTheme(Theme.Dark);
            application.Run();
#endif

        }

        Theme? currentTheme = null;
        public void ChangeTheme(Theme theme)
        {
            if (currentTheme.HasValue && currentTheme.Value == theme)
                return;

            ThemeDictionary.MergedDictionaries.Clear();
            ThemeDictionary.MergedDictionaries.Add(new ResourceDictionary() { Source = ThemeSelector.GetThemeUrl(theme) });
            currentTheme = theme;
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // handle command line arguments of second instance
            // …

            return true;
        }

#endregion
    }
}
