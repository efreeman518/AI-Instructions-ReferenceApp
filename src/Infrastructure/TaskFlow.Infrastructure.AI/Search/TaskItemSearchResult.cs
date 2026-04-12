namespace TaskFlow.Infrastructure.AI.Search;

public class TaskItemSearchResult
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public string? CategoryName { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public double? Score { get; set; }
}

public enum SearchMode
{
    Keyword,
    Semantic,
    Vector,
    Hybrid
}
