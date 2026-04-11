namespace TaskFlow.Domain.Model.ValueObjects;

public class DateRange
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
}
