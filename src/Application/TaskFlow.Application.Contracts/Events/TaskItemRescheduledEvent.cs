namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides task item rescheduled event behavior for the Application Events layer.</summary>
public record TaskItemRescheduledEvent(
    Guid TaskItemId,
    Guid TenantId,
    DateTimeOffset? NewStartDate,
    DateTimeOffset? NewDueDate);