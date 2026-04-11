using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

public class TaskItemSearchFilter
{
    public string? SearchTerm { get; set; }
    public TaskItemStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentTaskItemId { get; set; }
    public DateTimeOffset? DueBefore { get; set; }
    public DateTimeOffset? DueAfter { get; set; }
    public bool? IsOverdue { get; set; }
}
