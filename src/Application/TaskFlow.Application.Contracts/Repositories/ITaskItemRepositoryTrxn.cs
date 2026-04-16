using EF.Data.Contracts;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITaskItemRepositoryTrxn : IRepositoryBase
{
    Task<TaskItem?> GetTaskItemAsync(Guid id, bool inclChildren = true, CancellationToken ct = default);
    DomainResult<TaskItem> UpdateFromDto(TaskItem entity, TaskItemDto dto, RelatedDeleteBehavior relatedDeleteBehavior = RelatedDeleteBehavior.None);
}
