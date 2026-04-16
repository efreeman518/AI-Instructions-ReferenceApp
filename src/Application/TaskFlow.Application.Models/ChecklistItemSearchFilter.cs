namespace TaskFlow.Application.Models;

public record ChecklistItemSearchFilter : DefaultSearchFilter
{
    public Guid? TaskItemId { get; set; }
    public bool? IsCompleted { get; set; }
}
