using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace QuickShare.Rome
{
    public static class Extensions
    {
        public static ValueSet ToValueSet(this Dictionary<string, object> data)
        {
            ValueSet vs = new ValueSet();
            foreach (var item in data)
                vs.Add(item.Key, item.Value);
            return vs;
        }
    }
}
