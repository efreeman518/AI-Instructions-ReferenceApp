using System.Collections.ObjectModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

public sealed class NotificationService : INotificationService
{
    private static readonly TimeSpan SuccessDismiss = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan InfoDismiss    = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan WarningDismiss = TimeSpan.FromSeconds(6);

    private readonly TimeProvider _time;
    private readonly IUiDispatcher _dispatcher;

    public ObservableCollection<Notification> Items { get; } = [];

    public NotificationService() : this(IUiDispatcher.Inline, TimeProvider.System) { }

    public NotificationService(IUiDispatcher dispatcher) : this(dispatcher, TimeProvider.System) { }

    public NotificationService(IUiDispatcher dispatcher, TimeProvider time)
    {
        _dispatcher = dispatcher;
        _time = time;
    }

    public ValueTask ShowSuccess(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Success, message, title, AutoDismissAfter: SuccessDismiss));

    public ValueTask ShowInfo(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Info, message, title, AutoDismissAfter: InfoDismiss));

    public ValueTask ShowWarning(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Warning, message, title, AutoDismissAfter: WarningDismiss));

    public ValueTask ShowError(string message, string? title = null, CancellationToken ct = default) =>
        // Errors persist until user dismisses.
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Error, message, title));

    public ValueTask ShowProblem(ProblemDetailsPayload problem, CancellationToken ct = default)
    {
        var title = problem.Title ?? $"Request failed ({problem.Status})";
        var detail = problem.Detail ?? problem.Title ?? string.Empty;
        var key = $"{problem.Status}:{problem.Title}";
        return Add(new Notification(
            Guid.NewGuid(),
            NotificationSeverity.Error,
            detail,
            title,
            DedupeKey: key));
    }

    public ValueTask Dismiss(Guid id, CancellationToken ct = default)
    {
        OnUi(() => DismissCore(id));
        return default;
    }

    private void DismissCore(Guid id)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == id)
            {
                Items.RemoveAt(i);
                break;
            }
        }
    }

    private ValueTask Add(Notification n)
    {
        OnUi(() =>
        {
            if (n.DedupeKey is { } key)
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    if (Items[i].DedupeKey == key)
                    {
                        // Upsert — replace in-place with new content, keep position.
                        Items[i] = n;
                        return;
                    }
                }
            }
            Items.Add(n);
            if (n.AutoDismissAfter is { } delay)
            {
                _ = ScheduleDismiss(n.Id, delay);
            }
        });
        return default;
    }

    private async Task ScheduleDismiss(Guid id, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _time).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { return; }
        await Dismiss(id).ConfigureAwait(false);
    }

    private void OnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess) action();
        else _dispatcher.Post(action);
    }
}
