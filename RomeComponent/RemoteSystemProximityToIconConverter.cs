using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace QuickShare.Rome
{
    public class RemoteSystemProximityToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((value is bool))
            {
                var IsAvailableByProximity = (bool)value;

                if (IsAvailableByProximity)
                    return Char.ConvertFromUtf32(0xE957);
                else
                    return Char.ConvertFromUtf32(0xE753);
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
