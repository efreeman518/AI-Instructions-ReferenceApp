using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITaskItemRepositoryTrxn : IRepositoryBase
{
    Task<TaskItem?> GetTaskItemAsync(Guid id, CancellationToken ct = default);
}
