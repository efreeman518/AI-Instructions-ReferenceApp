using TaskFlow.Domain.Shared;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Domain.Model.Events;

public record TaskItemStatusChangedEvent(Guid TaskItemId, Guid TenantId, TaskItemStatus OldStatus, TaskItemStatus NewStatus) : IDomainEvent;
