using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i checklist item data through infrastructure storage contracts.</summary>
public interface IChecklistItemRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default);
}
