using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface IChecklistItemService
{
    Task<PagedResponse<ChecklistItemDto>> SearchAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<ChecklistItemDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<ChecklistItemDto>>> CreateAsync(DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<ChecklistItemDto>>> UpdateAsync(DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
