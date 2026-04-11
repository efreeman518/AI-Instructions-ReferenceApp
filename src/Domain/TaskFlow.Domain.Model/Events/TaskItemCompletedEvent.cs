using TaskFlow.Domain.Shared;

namespace TaskFlow.Domain.Model.Events;

public record TaskItemCompletedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset CompletedDate) : IDomainEvent;
