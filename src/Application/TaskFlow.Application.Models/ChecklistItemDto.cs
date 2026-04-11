namespace TaskFlow.Application.Models;

public class ChecklistItemDto
{
    public Guid? Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public Guid TaskItemId { get; set; }
}
