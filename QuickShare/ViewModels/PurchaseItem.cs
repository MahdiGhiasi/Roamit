using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace QuickShare.ViewModels
{
    public class PurchaseItem
    {
        public string StoreID { get; internal set; }
        public StorePrice Price { get; internal set; }
        public string Token { get; internal set; }

        public override string ToString()
        {
            return Price.FormattedPrice;
        }
    }
}
