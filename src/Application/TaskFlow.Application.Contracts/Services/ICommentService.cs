using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

public interface ICommentService
{
    Task<PagedResponse<CommentDto>> SearchAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<CommentDto>>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<DefaultResponse<CommentDto>>> CreateAsync(DefaultRequest<CommentDto> request, CancellationToken ct = default);
    Task<Result<DefaultResponse<CommentDto>>> UpdateAsync(DefaultRequest<CommentDto> request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
