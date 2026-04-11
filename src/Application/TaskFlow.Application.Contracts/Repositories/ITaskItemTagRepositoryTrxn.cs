using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITaskItemTagRepositoryTrxn : IRepositoryBase
{
    Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default);
}
