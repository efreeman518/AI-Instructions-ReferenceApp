using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i checklist item data through infrastructure storage contracts.</summary>
public interface IChecklistItemRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search checklist items and returns filtered results for callers.</summary>
    Task<PagedResponse<ChecklistItemDto>> SearchChecklistItemsAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default);
}
