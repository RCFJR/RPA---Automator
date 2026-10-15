using System.Globalization;
using System.Windows.Data;
using RPA.Core.Models;

namespace RPA.App.Helpers;

[ValueConversion(typeof(ShellType), typeof(string))]
public class ShellTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ShellType shellType ? shellType switch
        {
            ShellType.PowerShell => "PowerShell",
            ShellType.Bash => "Bash",
            _ => "Cmd"
        } : "Cmd";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string str ? str switch
        {
            "PowerShell" => ShellType.PowerShell,
            "Bash" => ShellType.Bash,
            _ => ShellType.Cmd
        } : ShellType.Cmd;
    }
}
