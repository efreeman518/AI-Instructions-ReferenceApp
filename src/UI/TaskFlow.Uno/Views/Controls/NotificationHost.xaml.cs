using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Views.Controls;

/// <summary>Hosts the notification host XAML view and initializes its Uno page or control.</summary>
public sealed partial class NotificationHost : UserControl
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(ObservableCollection<Notification>),
        typeof(NotificationHost),
        new PropertyMetadata(null));

    /// <summary>Initializes notification host with required dependencies and default state.</summary>
    public NotificationHost()
    {
        this.InitializeComponent();
    }

    public ObservableCollection<Notification>? Items
    {
        get => (ObservableCollection<Notification>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>Handles info bar closed events for notification host.</summary>
    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        // Only react to user-initiated closes - programmatic dismiss via the
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
