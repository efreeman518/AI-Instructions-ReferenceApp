namespace TaskFlow.Domain.Shared.Enums;

[Flags]
public enum TaskFeatures
{
    None = 0,
    Recurring = 1,
    Reminder = 2,
    SharedLink = 4
}
