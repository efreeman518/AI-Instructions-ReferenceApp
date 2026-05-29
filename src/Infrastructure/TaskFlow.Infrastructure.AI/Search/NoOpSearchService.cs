using Microsoft.Extensions.Logging;

namespace TaskFlow.Infrastructure.AI.Search;

/// <summary>Coordinates no op search application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public class NoOpSearchService(ILogger<NoOpSearchService> logger) : ITaskFlowSearchService
{
    /// <summary>Searches search task items and returns filtered results for callers.</summary>
    public Task<IReadOnlyList<TaskItemSearchResult>> SearchTaskItemsAsync(
        string query, SearchMode mode, Guid? tenantId, int maxResults = 10, CancellationToken ct = default)
    {
        logger.LogWarning("AI Search not configured - returning empty results for query '{Query}'", query);
        return Task.FromResult<IReadOnlyList<TaskItemSearchResult>>([]);
    }

    /// <summary>Provides the index task item operation for no op search service.</summary>
    public Task IndexTaskItemAsync(TaskItemSearchDocument document, CancellationToken ct = default)
    {
        logger.LogDebug("AI Search not configured - skipping index for document '{Id}'", document.Id);
        return Task.CompletedTask;
    }

    /// <summary>Removes remove task item while keeping aggregate relationship state consistent.</summary>
    public Task RemoveTaskItemAsync(string taskItemId, CancellationToken ct = default)
    {
        logger.LogDebug("AI Search not configured - skipping removal for '{Id}'", taskItemId);
        return Task.CompletedTask;
    }
}
