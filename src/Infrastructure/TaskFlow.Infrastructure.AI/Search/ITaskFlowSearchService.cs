namespace TaskFlow.Infrastructure.AI.Search;

/// <summary>Coordinates i task flow search application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ITaskFlowSearchService
{
    /// <summary>Searches search task items and returns filtered results for callers.</summary>
    Task<IReadOnlyList<TaskItemSearchResult>> SearchTaskItemsAsync(
        string query, SearchMode mode, Guid? tenantId, int maxResults = 10, CancellationToken ct = default);

    /// <summary>Provides the index task item operation for task flow search service.</summary>
    Task IndexTaskItemAsync(TaskItemSearchDocument document, CancellationToken ct = default);

    /// <summary>Removes remove task item while keeping aggregate relationship state consistent.</summary>
    Task RemoveTaskItemAsync(string taskItemId, CancellationToken ct = default);
}
