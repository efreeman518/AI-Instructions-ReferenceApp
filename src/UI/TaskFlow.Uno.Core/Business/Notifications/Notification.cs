namespace TaskFlow.Uno.Core.Business.Notifications;

public sealed record Notification(
    Guid Id,
    NotificationSeverity Severity,
    string Message,
    string? Title = null,
    string? DedupeKey = null,
    TimeSpan? AutoDismissAfter = null);
