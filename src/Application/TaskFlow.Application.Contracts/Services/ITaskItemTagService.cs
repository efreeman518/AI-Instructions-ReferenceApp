using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITaskItemTagService
{
    Task<Result<TaskItemTagDto>> CreateAsync(TaskItemTagDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<TaskItemTagDto>> GetAsync(Guid id, CancellationToken ct = default);
}
