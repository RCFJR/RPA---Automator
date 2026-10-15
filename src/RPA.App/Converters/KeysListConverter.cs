using System.Globalization;
using System.Windows.Data;

namespace RPA.App.Converters;

public class KeysListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is List<string> keys && keys.Count > 0)
        {
            return string.Join("+", keys);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            var keys = text.Split('+').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
            return keys;
        }
        return new List<string>();
    }
}
