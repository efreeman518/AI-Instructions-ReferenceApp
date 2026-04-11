using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface IChecklistItemService
{
    Task<Result<ChecklistItemDto>> CreateAsync(ChecklistItemDto dto, CancellationToken ct = default);
    Task<Result<ChecklistItemDto>> UpdateAsync(ChecklistItemDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<ChecklistItemDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<ChecklistItemDto>>> SearchAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default);
}
