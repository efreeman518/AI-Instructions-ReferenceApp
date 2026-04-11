using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITaskItemTagRepositoryQuery : IRepositoryBase
{
    Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default);
    Task<List<TaskItemTag>> GetByTaskItemIdAsync(Guid taskItemId, CancellationToken ct = default);
}
