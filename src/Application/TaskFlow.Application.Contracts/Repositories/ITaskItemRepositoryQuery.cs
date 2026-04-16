using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ITaskItemRepositoryQuery : IRepositoryBase
{
    Task<TaskItem?> GetTaskItemAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<TaskItemDto>> SearchTaskItemsAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default);
}
