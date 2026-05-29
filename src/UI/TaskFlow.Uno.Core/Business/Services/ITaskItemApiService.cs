using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates i task item API application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ITaskItemApiService
{
    /// <summary>Searches search and returns filtered results for callers.</summary>
    Task<IReadOnlyList<TaskItemModel>> SearchAsync(string? searchTerm = null, string? status = null,
        string? priority = null, Guid? categoryId = null, CancellationToken ct = default);
    /// <summary>Searches search page and returns filtered results for callers.</summary>
    Task<TaskItemSearchPage> SearchPageAsync(string? searchTerm = null, string? status = null,
        string? priority = null, Guid? categoryId = null, int pageNumber = 1,
        int pageSize = 10, CancellationToken ct = default);
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<TaskItemModel?> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    Task<TaskItemModel> CreateAsync(TaskItemModel model, CancellationToken ct = default);
    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    Task<TaskItemModel> UpdateAsync(TaskItemModel model, CancellationToken ct = default);
    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
