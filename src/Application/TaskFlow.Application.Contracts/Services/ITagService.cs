using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITagService
{
    Task<PagedResponse<TagDto>> SearchAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<TagDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<TagDto>>> CreateAsync(DefaultRequest<TagDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<TagDto>>> UpdateAsync(DefaultRequest<TagDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
