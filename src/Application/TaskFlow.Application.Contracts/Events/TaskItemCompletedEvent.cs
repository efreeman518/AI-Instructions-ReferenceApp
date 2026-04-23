namespace TaskFlow.Application.Contracts.Events;

public record TaskItemCompletedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset CompletedDate);