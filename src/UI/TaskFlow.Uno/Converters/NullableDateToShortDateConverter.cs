using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace TaskFlow.Uno.Converters;

public sealed class NullableDateToShortDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is DateTimeOffset dto
            ? dto.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)
            : "Not set";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}