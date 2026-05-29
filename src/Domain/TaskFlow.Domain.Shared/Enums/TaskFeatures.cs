namespace TaskFlow.Domain.Shared.Enums;

/// <summary>Defines the supported task features values shared across TaskFlow layers.</summary>
[Flags]
public enum TaskFeatures
{
    None = 0,
    Recurring = 1,
    Reminder = 2,
    SharedLink = 4
}
