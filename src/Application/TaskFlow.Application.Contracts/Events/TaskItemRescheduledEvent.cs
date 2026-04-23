namespace TaskFlow.Application.Contracts.Events;

public record TaskItemRescheduledEvent(
    Guid TaskItemId,
    Guid TenantId,
    DateTimeOffset? NewStartDate,
    DateTimeOffset? NewDueDate);