using QuickShare.HelperClasses.Version;
using QuickShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace QuickShare.Flyouts
{
    public sealed partial class DonateFlyout : UserControl, IFlyout
    {
        public event EventHandler FlyoutCloseRequest;

        public DonateFlyout()
        {
            this.InitializeComponent();
        }

        public async void InitFlyout()
        {
            DonateButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            PleaseWaitProgressRing.IsActive = false;
            PleaseWaitProgressRing.Visibility = Visibility.Collapsed;
            PricesLoadingProgressRing.IsActive = true;

            await LoadPrices();

            PricesLoadingProgressRing.IsActive = false;
            DonateButton.IsEnabled = true;
        }

        private async Task LoadPrices()
        {
            if (donatePricesList.Items.Count != 0)
                return;

            List<PurchaseItem> items = await StoreHelper.GetDonateItems();

            foreach (var item in items)
                donatePricesList.Items.Add(item);

            if (items.Count > 0)
                donatePricesList.SelectedIndex = 0;
        }

        private void Cancel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private async void Donate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            EnableProgressRing();

            if (donatePricesList.SelectedItem is PurchaseItem item)
            {
                var result = await StoreHelper.TryPurchaseConsumable(item);

                if (result == Windows.Services.Store.StorePurchaseStatus.Succeeded)
                {
                    MessageDialog md = new MessageDialog("Thank you for your support!");
                    await md.ShowAsync();
                }
                else
                {
                    MessageDialog md = new MessageDialog(result.ToString(), "Purchase failed.");
                    await md.ShowAsync();
                }
            }

            FlyoutCloseRequest?.Invoke(this, new EventArgs());
        }

        private void EnableProgressRing()
        {
            DonateButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            PleaseWaitProgressRing.Visibility = Visibility.Visible;
            PleaseWaitProgressRing.IsActive = true;
        }

        private async void CloudClipboardLearnMore_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(Common.Constants.PCExtensionUrl));
        }
    }
}
