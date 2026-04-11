namespace TaskFlow.Domain.Shared.Enums;

public enum TaskItemStatus
{
    None = 0,
    Open = 1,
    InProgress = 2,
    Blocked = 3,
    Completed = 4,
    Cancelled = 5
}
