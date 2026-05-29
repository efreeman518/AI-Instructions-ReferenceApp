using EF.Data.Contracts;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i task item data through infrastructure storage contracts.</summary>
public interface ITaskItemRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<TaskItem?> GetTaskItemAsync(Guid id, bool inclChildren = true, CancellationToken ct = default);
    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    DomainResult<TaskItem> UpdateFromDto(TaskItem entity, TaskItemDto dto, RelatedDeleteBehavior relatedDeleteBehavior = RelatedDeleteBehavior.None);
}
