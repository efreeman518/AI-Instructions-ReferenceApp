using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITaskItemTagService
{
    Task<Result<DefaultResponse<TaskItemTagDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<TaskItemTagDto>>> CreateAsync(DefaultRequest<TaskItemTagDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
