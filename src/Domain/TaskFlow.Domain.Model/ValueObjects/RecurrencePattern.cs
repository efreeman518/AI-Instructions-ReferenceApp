namespace TaskFlow.Domain.Model.ValueObjects;

public class RecurrencePattern
{
    public int Interval { get; set; }
    public string Frequency { get; set; } = null!;
    public DateTimeOffset? EndDate { get; set; }
}
