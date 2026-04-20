using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Views.Controls;

public sealed partial class NotificationHost : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(ObservableCollection<Notification>),
        typeof(NotificationHost),
        new PropertyMetadata(null));

    public NotificationHost()
    {
        this.InitializeComponent();
    }

    public ObservableCollection<Notification>? Items
    {
        get => (ObservableCollection<Notification>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        // Only react to user-initiated closes — programmatic dismiss via the
        // notification service removes the item from Items directly, which
        // would re-enter this handler with Reason=Programmatic otherwise.
        if (args.Reason != InfoBarCloseReason.CloseButton) return;

        if (sender.Tag is Guid id && Items is { } items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    items.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
