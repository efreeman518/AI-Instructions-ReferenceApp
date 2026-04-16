namespace TaskFlow.Application.Models;

public record CommentSearchFilter : DefaultSearchFilter
{
    public Guid? TaskItemId { get; set; }
}
