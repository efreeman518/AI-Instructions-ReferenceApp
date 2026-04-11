namespace TaskFlow.Domain.Model.ValueObjects;

public class RecurrencePattern
{
    public int Interval { get; init; }
    public string Frequency { get; init; } = null!;
    public DateTimeOffset? EndDate { get; init; }
}
