namespace TaskFlow.Application.Models;

public class CommentSearchFilter
{
    public string? SearchTerm { get; set; }
    public Guid? TaskItemId { get; set; }
}
