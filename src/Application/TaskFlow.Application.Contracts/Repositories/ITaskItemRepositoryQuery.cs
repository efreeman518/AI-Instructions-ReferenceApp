using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i task item data through infrastructure storage contracts.</summary>
public interface ITaskItemRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<TaskItem?> GetTaskItemAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search task items and returns filtered results for callers.</summary>
    Task<PagedResponse<TaskItemDto>> SearchTaskItemsAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default);
}
