namespace TaskFlow.Domain.Model.ValueObjects;

public class DateRange
{
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
}
