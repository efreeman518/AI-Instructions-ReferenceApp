using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITaskItemService
{
    Task<PagedResponse<TaskItemDto>> SearchAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<TaskItemDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<TaskItemDto>>> CreateAsync(DefaultRequest<TaskItemDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<TaskItemDto>>> UpdateAsync(DefaultRequest<TaskItemDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
