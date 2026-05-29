using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i comment data through infrastructure storage contracts.</summary>
public interface ICommentRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default);
}
