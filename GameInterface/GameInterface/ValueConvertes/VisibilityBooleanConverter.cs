using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GameInterface.ValueConvertes
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class VisibilityBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is bool)) return Visibility.Visible;
            if ((bool)value == false)
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Visibility)) return true;
            if ((Visibility)value == Visibility.Visible)
                return true;
            else
                return false;
        }
    }
}
