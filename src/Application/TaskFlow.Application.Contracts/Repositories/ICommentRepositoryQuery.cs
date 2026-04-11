using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

public interface ICommentRepositoryQuery : IRepositoryBase
{
    Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<Comment>> SearchCommentsAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default);
}
