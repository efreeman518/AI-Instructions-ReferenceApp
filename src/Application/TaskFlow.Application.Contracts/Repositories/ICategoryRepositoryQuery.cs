using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i category data through infrastructure storage contracts.</summary>
public interface ICategoryRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search categories and returns filtered results for callers.</summary>
    Task<PagedResponse<CategoryDto>> SearchCategoriesAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default);
}
