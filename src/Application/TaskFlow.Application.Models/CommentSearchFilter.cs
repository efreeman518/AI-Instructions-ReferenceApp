namespace TaskFlow.Application.Models;

public class CommentSearchFilter
{
    public Guid? TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public Guid? TaskItemId { get; set; }
}
