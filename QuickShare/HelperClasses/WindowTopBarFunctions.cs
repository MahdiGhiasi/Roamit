using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace QuickShare.HelperClasses
{
    internal static class WindowTopBarFunctions
    {
        internal static void ApplyAcrylic()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            formattableTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        internal static void DisableAcrylic()
        {
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = formattableTitleBar.BackgroundColor;
            formattableTitleBar.ButtonInactiveBackgroundColor = formattableTitleBar.InactiveBackgroundColor;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = false;
        }
    }
}
