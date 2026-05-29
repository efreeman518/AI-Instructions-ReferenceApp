namespace TaskFlow.Domain.Shared.Enums;

/// <summary>Defines the supported task item status values shared across TaskFlow layers.</summary>
public enum TaskItemStatus
{
    None = 0,
    Open = 1,
    InProgress = 2,
    Blocked = 3,
    Completed = 4,
    Cancelled = 5
}
