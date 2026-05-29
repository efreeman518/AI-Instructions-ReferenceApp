using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i tag data through infrastructure storage contracts.</summary>
public interface ITagRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default);
}
