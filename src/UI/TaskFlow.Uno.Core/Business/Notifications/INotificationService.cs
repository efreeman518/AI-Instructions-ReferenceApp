using System.Collections.ObjectModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Coordinates i notification application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface INotificationService
{
    ObservableCollection<Notification> Items { get; }

    /// <summary>Provides the show success operation for notification service.</summary>
    ValueTask ShowSuccess(string message, string? title = null, CancellationToken ct = default);
    /// <summary>Provides the show info operation for notification service.</summary>
    ValueTask ShowInfo(string message, string? title = null, CancellationToken ct = default);
    /// <summary>Provides the show warning operation for notification service.</summary>
    ValueTask ShowWarning(string message, string? title = null, CancellationToken ct = default);
    /// <summary>Provides the show error operation for notification service.</summary>
    ValueTask ShowError(string message, string? title = null, CancellationToken ct = default);

    /// <summary>Provides the show problem operation for notification service.</summary>
    ValueTask ShowProblem(ProblemDetailsPayload problem, CancellationToken ct = default);

    /// <summary>Dismisses dismiss for the active view model.</summary>
    ValueTask Dismiss(Guid id, CancellationToken ct = default);
}
