using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace QuickShare.Classes.Converters
{
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) => !((bool)value);
        public object ConvertBack(object value, Type targetType, object parameter, string language) => !((bool)value);
    }
}
