namespace TaskFlow.Application.Contracts.Events;

public record TaskItemCreatedEvent(Guid TaskItemId, Guid TenantId, string Title);