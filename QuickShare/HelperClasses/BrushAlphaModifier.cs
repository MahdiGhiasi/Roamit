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
            SolidColorBrush originalBrush = value as SolidColorBrush;

            if (originalBrush == null)
                throw new Exception();

            Brush newBrush = new SolidColorBrush(originalBrush.Color)
            {
                Opacity = double.Parse(parameter.ToString()),
            };
            return newBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
