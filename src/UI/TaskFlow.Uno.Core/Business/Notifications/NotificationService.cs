using System.Collections.ObjectModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Coordinates notification application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public sealed class NotificationService : INotificationService
{
    private static readonly TimeSpan SuccessDismiss = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan InfoDismiss = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan WarningDismiss = TimeSpan.FromSeconds(6);

    private readonly TimeProvider _time;
    private readonly IUiDispatcher _dispatcher;

    public ObservableCollection<Notification> Items { get; } = [];

    /// <summary>Initializes notification service with required dependencies and default state.</summary>
    public NotificationService() : this(IUiDispatcher.Inline, TimeProvider.System) { }

    /// <summary>Initializes notification service with required dependencies and default state.</summary>
    public NotificationService(IUiDispatcher dispatcher) : this(dispatcher, TimeProvider.System) { }

    /// <summary>Initializes notification service with required dependencies and default state.</summary>
    public NotificationService(IUiDispatcher dispatcher, TimeProvider time)
    {
        _dispatcher = dispatcher;
        _time = time;
    }

    /// <summary>Provides the show success operation for notification service.</summary>
    public ValueTask ShowSuccess(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Success, message, title, AutoDismissAfter: SuccessDismiss));

    /// <summary>Provides the show info operation for notification service.</summary>
    public ValueTask ShowInfo(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Info, message, title, AutoDismissAfter: InfoDismiss));

    /// <summary>Provides the show warning operation for notification service.</summary>
    public ValueTask ShowWarning(string message, string? title = null, CancellationToken ct = default) =>
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Warning, message, title, AutoDismissAfter: WarningDismiss));

    /// <summary>Provides the show error operation for notification service.</summary>
    public ValueTask ShowError(string message, string? title = null, CancellationToken ct = default) =>
        // Errors persist until user dismisses.
        Add(new Notification(Guid.NewGuid(), NotificationSeverity.Error, message, title));

    /// <summary>Provides the show problem operation for notification service.</summary>
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

    /// <summary>Dismisses dismiss for the active view model.</summary>
    public ValueTask Dismiss(Guid id, CancellationToken ct = default)
    {
        OnUi(() => DismissCore(id));
        return default;
    }

    /// <summary>Dismisses dismiss core for the active view model.</summary>
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

    /// <summary>Registers TaskFlow dependencies in the service container.</summary>
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
                        // Upsert - replace in-place with new content, keep position.
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

    /// <summary>Provides the schedule dismiss operation for notification service.</summary>
    private async Task ScheduleDismiss(Guid id, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _time).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { return; }
        await Dismiss(id).ConfigureAwait(false);
    }

    /// <summary>Handles UI events for notification service.</summary>
    private void OnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess) action();
        else _dispatcher.Post(action);
    }
}
