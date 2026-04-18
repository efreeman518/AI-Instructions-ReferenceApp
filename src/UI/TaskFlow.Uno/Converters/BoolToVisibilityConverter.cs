using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace TaskFlow.Uno.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var boolValue = value is true;
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var isVisible = value is Visibility v && v == Visibility.Visible;
        return invert ? !isVisible : isVisible;
    }
}
