namespace TaskFlow.Infrastructure.AI.Search;

public interface ITaskFlowSearchService
{
    Task<IReadOnlyList<TaskItemSearchResult>> SearchTaskItemsAsync(
        string query, SearchMode mode, Guid? tenantId, int maxResults = 10, CancellationToken ct = default);

    Task IndexTaskItemAsync(TaskItemSearchDocument document, CancellationToken ct = default);

    Task RemoveTaskItemAsync(string taskItemId, CancellationToken ct = default);
}
