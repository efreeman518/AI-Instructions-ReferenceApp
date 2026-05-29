using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Presentation;

/// <summary>Drives main state, navigation, and commands for the Uno presentation layer.</summary>
public partial record MainModel
{
    /// <summary>Initializes main model with required dependencies and default state.</summary>
    public MainModel(IBusyTracker busy, INotificationService notifications)
    {
        Busy = busy;
        Notifications = notifications;
    }

    public IBusyTracker Busy { get; }
    public INotificationService Notifications { get; }

    /// <summary>Dismisses dismiss for the active view model.</summary>
    public async ValueTask Dismiss(Guid id) => await Notifications.Dismiss(id);
}
