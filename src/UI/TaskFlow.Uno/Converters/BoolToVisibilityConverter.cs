using Microsoft.UI.Xaml.Data;

namespace TaskFlow.Uno.Converters;

/// <summary>Converts bool to visibility values for Uno XAML binding and display.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Converts values for XAML binding or display logic.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var boolValue = value is true;
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Converts values back from UI bindings when reverse conversion is supported.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        var isVisible = value is Visibility v && v == Visibility.Visible;
        return invert ? !isVisible : isVisible;
    }
}
