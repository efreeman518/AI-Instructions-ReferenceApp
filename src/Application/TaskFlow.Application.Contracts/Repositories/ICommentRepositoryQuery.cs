using EF.Common.Contracts;
using EF.Data.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Contracts.Repositories;

/// <summary>Persists and queries i comment data through infrastructure storage contracts.</summary>
public interface ICommentRepositoryQuery : IRepositoryBase
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default);
    /// <summary>Searches search comments and returns filtered results for callers.</summary>
    Task<PagedResponse<CommentDto>> SearchCommentsAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default);
}
