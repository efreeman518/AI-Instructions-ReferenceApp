using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Storage;

namespace TaskFlow.Infrastructure.Storage.CosmosDb;

/// <summary>Persists and queries cosmos task view data through infrastructure storage contracts.</summary>
public class CosmosTaskViewRepository : ITaskViewRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosTaskViewRepository> _logger;

    /// <summary>Initializes cosmos task view repository with required dependencies and default state.</summary>
    public CosmosTaskViewRepository(
        CosmosClient cosmosClient,
        ILogger<CosmosTaskViewRepository> logger,
        string databaseName,
        string containerName)
    {
        _logger = logger;
        var database = cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(containerName);
    }

    /// <summary>Writes upsert to the configured read model store.</summary>
    public async Task UpsertAsync(TaskViewDto taskView, CancellationToken ct = default)
    {
        var document = MapToDocument(taskView);
        await _container.UpsertItemAsync(document,
            new PartitionKey(document.TenantId), cancellationToken: ct);
        _logger.TaskViewUpserted(document.Id, document.TenantId);
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<TaskViewDto?> GetAsync(string id, string tenantId, CancellationToken ct = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<TaskViewDocument>(
                id, new PartitionKey(tenantId), cancellationToken: ct);
            return MapToDto(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Queries query by tenant from the configured read model store.</summary>
    public async Task<IReadOnlyList<TaskViewDto>> QueryByTenantAsync(
        string tenantId, int pageSize = 20, string? continuationToken = null,
        CancellationToken ct = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId ORDER BY c.lastModifiedUtc DESC")
            .WithParameter("@tenantId", tenantId);

        var options = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(tenantId),
            MaxItemCount = pageSize
        };

        var results = new List<TaskViewDto>();
        using var iterator = _container.GetItemQueryIterator<TaskViewDocument>(query, continuationToken, options);

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            foreach (var doc in response)
            {
                results.Add(MapToDto(doc));
            }
        }

        return results;
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    public async Task DeleteAsync(string id, string tenantId, CancellationToken ct = default)
    {
        try
        {
            await _container.DeleteItemAsync<TaskViewDocument>(
                id, new PartitionKey(tenantId), cancellationToken: ct);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.TaskViewNotFoundForDeletion(id);
        }
    }

    /// <summary>Maps to document into the target contract used by callers.</summary>
    private static TaskViewDocument MapToDocument(TaskViewDto dto) => new()
    {
        Id = dto.Id,
        TenantId = dto.TenantId,
        Title = dto.Title,
        Description = dto.Description,
        Status = dto.Status,
        Priority = dto.Priority,
        CategoryName = dto.CategoryName,
        StartDate = dto.StartDate,
        DueDate = dto.DueDate,
        CompletedDate = dto.CompletedDate,
        IsOverdue = dto.IsOverdue,
        Tags = dto.Tags,
        CommentCount = dto.CommentCount,
        ChecklistTotal = dto.ChecklistTotal,
        ChecklistCompleted = dto.ChecklistCompleted,
        AttachmentCount = dto.AttachmentCount,
        SubTaskCount = dto.SubTaskCount,
        LastModifiedUtc = dto.LastModifiedUtc,
        CreatedUtc = dto.CreatedUtc
    };

    /// <summary>Maps to DTO into the target contract used by callers.</summary>
    private static TaskViewDto MapToDto(TaskViewDocument doc) => new()
    {
        Id = doc.Id,
        TenantId = doc.TenantId,
        Title = doc.Title,
        Description = doc.Description,
        Status = doc.Status,
        Priority = doc.Priority,
        CategoryName = doc.CategoryName,
        StartDate = doc.StartDate,
        DueDate = doc.DueDate,
        CompletedDate = doc.CompletedDate,
        IsOverdue = doc.IsOverdue,
        Tags = doc.Tags,
        CommentCount = doc.CommentCount,
        ChecklistTotal = doc.ChecklistTotal,
        ChecklistCompleted = doc.ChecklistCompleted,
        AttachmentCount = doc.AttachmentCount,
        SubTaskCount = doc.SubTaskCount,
        LastModifiedUtc = doc.LastModifiedUtc,
        CreatedUtc = doc.CreatedUtc
    };
}
