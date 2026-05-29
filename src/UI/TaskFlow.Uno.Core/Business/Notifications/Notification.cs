namespace TaskFlow.Uno.Core.Business.Notifications;

/// <summary>Represents or dispatches notification state for the Uno client.</summary>
public sealed record Notification(
    Guid Id,
    NotificationSeverity Severity,
    string Message,
    string? Title = null,
    string? DedupeKey = null,
    TimeSpan? AutoDismissAfter = null);
