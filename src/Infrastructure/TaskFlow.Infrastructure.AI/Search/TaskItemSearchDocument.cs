namespace TaskFlow.Infrastructure.AI.Search;

public class TaskItemSearchDocument
{
    public string Id { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string? CategoryName { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Combined text field for embedding generation (Title + Description).
    /// </summary>
    public string? ContentVector { get; set; }
}
