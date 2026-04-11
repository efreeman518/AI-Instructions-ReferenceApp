using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ICategoryService
{
    Task<PagedResponse<CategoryDto>> SearchAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<CategoryDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<CategoryDto>>> CreateAsync(DefaultRequest<CategoryDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<CategoryDto>>> UpdateAsync(DefaultRequest<CategoryDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
