using EF.Data.Contracts;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i category data through infrastructure storage contracts.</summary>
public interface ICategoryRepositoryTrxn : IRepositoryTrxn<Category, CategoryId>
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default);
}
