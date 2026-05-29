namespace TaskFlow.Domain.Model.ValueObjects;

/// <summary>Models date range domain behavior and invariants.</summary>
public class DateRange
{
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
}
