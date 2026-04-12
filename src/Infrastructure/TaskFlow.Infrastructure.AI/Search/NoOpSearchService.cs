using Microsoft.Extensions.Logging;

namespace TaskFlow.Infrastructure.AI.Search;

public class NoOpSearchService(ILogger<NoOpSearchService> logger) : ITaskFlowSearchService
{
    public Task<IReadOnlyList<TaskItemSearchResult>> SearchTaskItemsAsync(
        string query, SearchMode mode, Guid? tenantId, int maxResults = 10, CancellationToken ct = default)
    {
        logger.LogWarning("AI Search not configured — returning empty results for query '{Query}'", query);
        return Task.FromResult<IReadOnlyList<TaskItemSearchResult>>([]);
    }

    public Task IndexTaskItemAsync(TaskItemSearchDocument document, CancellationToken ct = default)
    {
        logger.LogDebug("AI Search not configured — skipping index for document '{Id}'", document.Id);
        return Task.CompletedTask;
    }

    public Task RemoveTaskItemAsync(string taskItemId, CancellationToken ct = default)
    {
        logger.LogDebug("AI Search not configured — skipping removal for '{Id}'", taskItemId);
        return Task.CompletedTask;
    }
}
