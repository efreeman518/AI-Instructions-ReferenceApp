using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ITagService
{
    Task<Result<TagDto>> CreateAsync(TagDto dto, CancellationToken ct = default);
    Task<Result<TagDto>> UpdateAsync(TagDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<TagDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<TagDto>>> SearchAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default);
}
