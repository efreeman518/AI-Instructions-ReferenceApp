namespace TaskFlow.Application.Models;

public class ChecklistItemSearchFilter
{
    public string? SearchTerm { get; set; }
    public Guid? TaskItemId { get; set; }
    public bool? IsCompleted { get; set; }
}
