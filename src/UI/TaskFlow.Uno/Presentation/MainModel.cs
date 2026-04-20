using TaskFlow.Uno.Core.Business.Notifications;

namespace TaskFlow.Uno.Presentation;

public partial record MainModel
{
    public MainModel(IBusyTracker busy, INotificationService notifications)
    {
        Busy = busy;
        Notifications = notifications;
    }

    public IBusyTracker Busy { get; }
    public INotificationService Notifications { get; }

    public async ValueTask Dismiss(Guid id) => await Notifications.Dismiss(id);
}
