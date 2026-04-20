using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Converters;

public sealed class SeverityToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is NotificationSeverity severity
            ? severity switch
            {
                NotificationSeverity.Success => InfoBarSeverity.Success,
                NotificationSeverity.Warning => InfoBarSeverity.Warning,
                NotificationSeverity.Error   => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            }
            : InfoBarSeverity.Informational;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
