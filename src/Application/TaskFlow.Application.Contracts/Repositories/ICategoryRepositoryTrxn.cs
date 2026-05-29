using EF.Data.Contracts;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i category data through infrastructure storage contracts.</summary>
public interface ICategoryRepositoryTrxn : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default);
}
