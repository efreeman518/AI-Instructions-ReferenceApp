using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ICommentService
{
    Task<Result<CommentDto>> CreateAsync(CommentDto dto, CancellationToken ct = default);
    Task<Result<CommentDto>> UpdateAsync(CommentDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<CommentDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedResponse<CommentDto>>> SearchAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default);
}
