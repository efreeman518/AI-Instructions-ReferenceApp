using Microsoft.UI.Xaml.Data;

namespace TaskFlow.Uno.Converters;

public sealed class BoolToActiveTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? "Active" : "Inactive";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is string s && s.Equals("Active", StringComparison.OrdinalIgnoreCase);
    }
}
