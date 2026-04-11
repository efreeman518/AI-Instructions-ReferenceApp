namespace TaskFlow.Application.Models;

public class ChecklistItemSearchFilter
{
    public Guid? TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public Guid? TaskItemId { get; set; }
    public bool? IsCompleted { get; set; }
}
