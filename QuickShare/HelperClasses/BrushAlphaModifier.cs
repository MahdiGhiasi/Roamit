using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace QuickShare.Common
{
    public class BrushAlphaModifier : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Brush brush = value as Brush;

            if (brush == null)
                throw new Exception();

            brush.Opacity = double.Parse(parameter.ToString());

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
