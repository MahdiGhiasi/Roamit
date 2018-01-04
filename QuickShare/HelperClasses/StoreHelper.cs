using GoogleAnalytics;
using QuickShare.Common;
using QuickShare.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Popups;

namespace QuickShare.HelperClasses.Version
{
    internal static class StoreHelper
    {
        static readonly string RemoveAdsAndSizeLimit_Token = "RemoveAdsAndSizeLimit";
        static readonly string RemoveAdsAndSizeLimit_StoreID = "9msqqgzbc1s5";
        static readonly string Donate_TokenBegin = "Donate";

        public delegate void ShowUpgradeFlyoutEventHandler(UpgradeFlyoutState state);
        public static event ShowUpgradeFlyoutEventHandler ShowUpgradeFlyout;

        private static StoreContext context = null;

        public static TaskCompletionSource<bool> UpgradeFlyoutCompletion;

        internal static async Task AskForUpgradeWhileSending()
        {
            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.WhileSendingFile);
            await UpgradeFlyoutCompletion.Task;
        }

        internal static async Task AskForUpgrade()
        {
            if (ShowUpgradeFlyout == null)
                return;

            UpgradeFlyoutCompletion = new TaskCompletionSource<bool>();
            ShowUpgradeFlyout.Invoke(UpgradeFlyoutState.Default);
            await UpgradeFlyoutCompletion.Task;
        }

        internal static async Task<List<PurchaseItem>> GetDonateItems()
        {
            List<PurchaseItem> items = new List<PurchaseItem>();

            if (context == null)
                context = StoreContext.GetDefault();

            StoreProductQueryResult queryResult = await context.GetAssociatedStoreProductsAsync(new string[] { "UnmanagedConsumable" });

            if (queryResult.ExtendedError != null)
            {
                return items;
            }

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products)
            {
                if ((item.Value.InAppOfferToken.Length >= (Donate_TokenBegin.Length + 1)) && (item.Value.InAppOfferToken.Substring(0, Donate_TokenBegin.Length) == Donate_TokenBegin))
                {
                    items.Add(new PurchaseItem
                    {
                        Price = item.Value.Price,
                        StoreID = item.Value.StoreId,
                        Token = item.Value.InAppOfferToken,
                    });
                }
            }

            return items;
        }

        internal static async Task<StorePurchaseStatus> TryPurchaseConsumable(PurchaseItem item)
        {
            if (context == null)
                context = StoreContext.GetDefault();

            Guid trackingId = Guid.NewGuid();

            try
            {
                StorePurchaseResult result = await context.RequestPurchaseAsync(item.StoreID);

                var fulfillResult = await context.ReportConsumableFulfillmentAsync(item.StoreID, 1, trackingId);

                Debug.WriteLine($"In app purchase of consumable {item.Token} finished with status: Purchase={result.Status}, Fulfill={fulfillResult.Status}");

#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("TryDonate" + item.Token, "Done", $"Purchase={result.Status}, Fulfill={fulfillResult.Status}").Build());
#endif

                return result.Status;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"In app purchase of consumable {item.Token} failed: {ex.Message}");
#if !DEBUG
                App.Tracker.Send(HitBuilder.CreateCustomEvent("TryDonate" + item.Token, "Failed", ex.Message).Build());
#endif
                return StorePurchaseStatus.NotPurchased;
            }
        }

        internal static async Task<StorePrice> GetUpgradePrice()
        {
            if (context == null)
                context = StoreContext.GetDefault();

            StoreProductQueryResult queryResult = await context.GetAssociatedStoreProductsAsync(new string[] { "Durable" });

            if (queryResult.ExtendedError != null)
            {
                return null;
            }

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products)
            {
                if (item.Value.InAppOfferToken == RemoveAdsAndSizeLimit_Token)
                {
                    return item.Value.Price;
                }
            }

            return null;
        }
    }

    public enum UpgradeFlyoutState
    {
        Default,
        WhileSendingFile
    }
}
