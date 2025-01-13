using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Views.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class VisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = (bool) value;
        if (isVisible) return Visibility.Visible;
        else return Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibility = (Visibility) value;

        if (visibility == Visibility.Visible) return true;
        else return false;
    }
}