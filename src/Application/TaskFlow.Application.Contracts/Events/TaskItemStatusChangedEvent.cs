using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Events;

public record TaskItemStatusChangedEvent(
    Guid TaskItemId,
    Guid TenantId,
    TaskItemStatus OldStatus,
    TaskItemStatus NewStatus);