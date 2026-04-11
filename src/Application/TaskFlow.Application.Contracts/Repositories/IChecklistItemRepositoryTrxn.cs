using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface IChecklistItemRepositoryTrxn : IRepositoryBase
{
    Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default);
}
