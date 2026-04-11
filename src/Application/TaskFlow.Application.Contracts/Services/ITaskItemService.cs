using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITaskItemService
{
    Task<Result<TaskItemDto>> CreateAsync(TaskItemDto dto, CancellationToken ct = default);
    Task<Result<TaskItemDto>> UpdateAsync(TaskItemDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaskItemDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<TaskItemDto>>> SearchAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default);
}
