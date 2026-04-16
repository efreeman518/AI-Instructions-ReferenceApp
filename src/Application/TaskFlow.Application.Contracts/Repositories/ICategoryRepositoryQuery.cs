using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ICategoryRepositoryQuery : IRepositoryBase
{
    Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<CategoryDto>> SearchCategoriesAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default);
}
