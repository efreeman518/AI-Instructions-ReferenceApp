using TaskFlow.Domain.Shared;

namespace TaskFlow.Domain.Model.Events;

public record TaskItemOverdueSuspectedEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset DueDate) : IDomainEvent;
