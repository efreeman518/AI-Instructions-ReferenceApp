namespace TaskFlow.Infrastructure.AI.Search;

/// <summary>Provides task item search result behavior for the Infrastructure Search layer.</summary>
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

/// <summary>Defines the supported search mode values shared across TaskFlow layers.</summary>
public enum SearchMode
{
    Keyword,
    Semantic,
    Vector,
    Hybrid
}
