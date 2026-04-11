using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ICategoryService
{
    Task<Result<CategoryDto>> CreateAsync(CategoryDto dto, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(CategoryDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<CategoryDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<CategoryDto>>> SearchAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default);
}
