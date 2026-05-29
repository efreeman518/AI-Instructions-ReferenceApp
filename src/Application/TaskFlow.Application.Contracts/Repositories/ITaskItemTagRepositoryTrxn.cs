using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i task item tag data through infrastructure storage contracts.</summary>
public interface ITaskItemTagRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default);
}
