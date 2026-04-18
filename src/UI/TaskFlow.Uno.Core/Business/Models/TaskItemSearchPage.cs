namespace TaskFlow.Uno.Core.Business.Models;

public record TaskItemSearchPage
{
    public IReadOnlyList<TaskItemModel> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
    public int StartItemNumber => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItemNumber => TotalCount == 0 ? 0 : Math.Min(TotalCount, StartItemNumber + Items.Count - 1);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasItems => Items.Count > 0;
    public bool IsEmpty => Items.Count == 0;
    public bool ShowPager => TotalPages > 1;
    public IReadOnlyList<int> VisiblePageNumbers => [.. GetVisiblePageNumbers()];

    private IEnumerable<int> GetVisiblePageNumbers()
    {
        if (TotalPages <= 1)
        {
            yield break;
        }

        var start = Math.Max(1, PageNumber - 2);
        var end = Math.Min(TotalPages, start + 4);
        start = Math.Max(1, end - 4);

        for (var pageNumber = start; pageNumber <= end; pageNumber++)
        {
            yield return pageNumber;
        }
    }
}