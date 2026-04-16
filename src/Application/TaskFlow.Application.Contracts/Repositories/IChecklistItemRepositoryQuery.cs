using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface IChecklistItemRepositoryQuery : IRepositoryBase
{
    Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<ChecklistItemDto>> SearchChecklistItemsAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default);
}
