using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides task item status changed event behavior for the Application Events layer.</summary>
public record TaskItemStatusChangedEvent(
    Guid TaskItemId,
    Guid TenantId,
    TaskItemStatus OldStatus,
    TaskItemStatus NewStatus);