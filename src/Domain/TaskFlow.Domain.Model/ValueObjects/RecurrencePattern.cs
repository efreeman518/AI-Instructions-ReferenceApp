namespace TaskFlow.Domain.Model.ValueObjects;

/// <summary>Models recurrence pattern domain behavior and invariants.</summary>
public class RecurrencePattern
{
    public int Interval { get; init; }
    public string Frequency { get; init; } = null!;
    public DateTimeOffset? EndDate { get; init; }
}
