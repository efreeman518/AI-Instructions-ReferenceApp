using Microsoft.UI.Xaml.Data;

namespace TaskFlow.Uno.Converters;

/// <summary>Converts bool to active text values for Uno XAML binding and display.</summary>
public sealed class BoolToActiveTextConverter : IValueConverter
{
    /// <summary>Converts values for XAML binding or display logic.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? "Active" : "Inactive";
    }

    /// <summary>Converts values back from UI bindings when reverse conversion is supported.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is string s && s.Equals("Active", StringComparison.OrdinalIgnoreCase);
    }
}
