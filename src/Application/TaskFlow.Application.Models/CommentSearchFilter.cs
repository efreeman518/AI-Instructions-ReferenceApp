namespace TaskFlow.Application.Models;

/// <summary>Provides comment search filter behavior for the Application layer.</summary>
public record CommentSearchFilter : DefaultSearchFilter
{
    public Guid? TaskItemId { get; set; }
}
