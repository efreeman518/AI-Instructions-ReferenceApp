using Microsoft.UI.Xaml.Data;
using System.Globalization;

namespace TaskFlow.Uno.Converters;

/// <summary>Converts nullable date to short date values for Uno XAML binding and display.</summary>
public sealed class NullableDateToShortDateConverter : IValueConverter
{
    /// <summary>Converts values for XAML binding or display logic.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is DateTimeOffset dto
            ? dto.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)
            : "Not set";
    }

    /// <summary>Converts values back from UI bindings when reverse conversion is supported.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}