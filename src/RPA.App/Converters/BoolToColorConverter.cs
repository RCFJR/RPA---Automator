using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RPA.App.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess ? new SolidColorBrush(Color.FromRgb(14, 124, 14)) : new SolidColorBrush(Color.FromRgb(209, 52, 56));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
