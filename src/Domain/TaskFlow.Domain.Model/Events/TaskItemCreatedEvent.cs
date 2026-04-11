using TaskFlow.Domain.Shared;

namespace TaskFlow.Domain.Model.Events;

public record TaskItemCreatedEvent(Guid TaskItemId, Guid TenantId, string Title) : IDomainEvent;
