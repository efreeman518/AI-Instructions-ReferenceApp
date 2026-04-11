using TaskFlow.Domain.Shared;

namespace TaskFlow.Domain.Model.Events;

public record TaskItemRescheduledEvent(Guid TaskItemId, Guid TenantId, DateTimeOffset? NewStartDate, DateTimeOffset? NewDueDate) : IDomainEvent;
