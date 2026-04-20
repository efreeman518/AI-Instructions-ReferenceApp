using System.Collections.ObjectModel;

namespace TaskFlow.Uno.Core.Business.Notifications;

public interface INotificationService
{
    ObservableCollection<Notification> Items { get; }

    ValueTask ShowSuccess(string message, string? title = null, CancellationToken ct = default);
    ValueTask ShowInfo   (string message, string? title = null, CancellationToken ct = default);
    ValueTask ShowWarning(string message, string? title = null, CancellationToken ct = default);
    ValueTask ShowError  (string message, string? title = null, CancellationToken ct = default);

    ValueTask ShowProblem(ProblemDetailsPayload problem, CancellationToken ct = default);

    ValueTask Dismiss(Guid id, CancellationToken ct = default);
}
