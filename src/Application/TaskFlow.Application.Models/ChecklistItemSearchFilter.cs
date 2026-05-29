namespace TaskFlow.Application.Models;

/// <summary>Provides checklist item search filter behavior for the Application layer.</summary>
public record ChecklistItemSearchFilter : DefaultSearchFilter
{
    public Guid? TaskItemId { get; set; }
    public bool? IsCompleted { get; set; }
}
