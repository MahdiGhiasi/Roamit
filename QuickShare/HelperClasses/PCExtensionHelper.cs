using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace QuickShare.HelperClasses
{
    internal static class PCExtensionHelper
    {
        internal static bool IsSupported
        {
            get
            {
                return (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0)); //(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
            }
        }

        internal static async Task StartPCExtension()
        {
            if (!IsSupported)
                return;

            App.PCExtensionCurrentPurpose = App.PCExtensionPurpose.Default;
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        internal static async Task StopPCExtensionIfRunning()
        {
            if (!IsSupported)
                return;

            App.PCExtensionCurrentPurpose = App.PCExtensionPurpose.Genocide;
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }
    }
}
