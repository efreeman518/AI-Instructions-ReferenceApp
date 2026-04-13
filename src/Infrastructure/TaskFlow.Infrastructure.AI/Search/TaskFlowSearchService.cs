using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaskFlow.Infrastructure.AI.Search;

#pragma warning disable CS9113 // Parameter 'settings' is unread — reserved for future AI configuration
public class TaskFlowSearchService(
    ILogger<TaskFlowSearchService> logger,
    SearchClient searchClient,
    IOptions<TaskFlowAiSettings> settings) : ITaskFlowSearchService
#pragma warning restore CS9113
{
    public async Task<IReadOnlyList<TaskItemSearchResult>> SearchTaskItemsAsync(
        string query, SearchMode mode, Guid? tenantId, int maxResults = 10, CancellationToken ct = default)
    {
        var options = BuildSearchOptions(mode, tenantId, maxResults, query);
        var searchText = mode == SearchMode.Vector ? null : query;
        var response = await searchClient.SearchAsync<TaskItemSearchDocument>(searchText, options, ct);

        var results = new List<TaskItemSearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(new TaskItemSearchResult
            {
                Id = result.Document.Id,
                Title = result.Document.Title,
                Description = result.Document.Description,
                Status = result.Document.Status,
                Priority = result.Document.Priority,
                CategoryName = result.Document.CategoryName,
                DueDate = result.Document.DueDate,
                Score = result.Score
            });
        }

        logger.LogDebug("AI Search returned {Count} results for query '{Query}' (mode={Mode})", results.Count, query, mode);
        return results;
    }

    public async Task IndexTaskItemAsync(TaskItemSearchDocument document, CancellationToken ct = default)
    {
        var batch = IndexDocumentsBatch.Upload([document]);
        await searchClient.IndexDocumentsAsync(batch, cancellationToken: ct);
        logger.LogDebug("Indexed task item '{Id}' in search", document.Id);
    }

    public async Task RemoveTaskItemAsync(string taskItemId, CancellationToken ct = default)
    {
        var batch = IndexDocumentsBatch.Delete("Id", [taskItemId]);
        await searchClient.IndexDocumentsAsync(batch, cancellationToken: ct);
        logger.LogDebug("Removed task item '{Id}' from search index", taskItemId);
    }

    private static SearchOptions BuildSearchOptions(SearchMode mode, Guid? tenantId, int maxResults, string query)
    {
        var options = new SearchOptions { Size = maxResults };

        if (tenantId.HasValue)
        {
            options.Filter = $"TenantId eq '{tenantId.Value}'";
        }

        switch (mode)
        {
            case SearchMode.Keyword:
                options.QueryType = SearchQueryType.Simple;
                break;

            case SearchMode.Semantic:
                options.QueryType = SearchQueryType.Semantic;
                options.SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "default"
                };
                break;

            case SearchMode.Vector:
                options.VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(query)
                        {
                            KNearestNeighborsCount = maxResults,
                            Fields = { "ContentVector" }
                        }
                    }
                };
                break;

            case SearchMode.Hybrid:
                options.QueryType = SearchQueryType.Semantic;
                options.SemanticSearch = new SemanticSearchOptions
                {
                    SemanticConfigurationName = "default"
                };
                options.VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(query)
                        {
                            KNearestNeighborsCount = maxResults,
                            Fields = { "ContentVector" }
                        }
                    }
                };
                break;
        }

        return options;
    }
}
