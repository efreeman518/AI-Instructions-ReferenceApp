using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Contracts.Services;

/// <summary>Coordinates i comment application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ICommentService
{
    /// <summary>Searches search and returns filtered results for callers.</summary>
    Task<PagedResponse<CommentDto>> SearchAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default);
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<Result<DefaultResponse<CommentDto>>> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    Task<Result<DefaultResponse<CommentDto>>> CreateAsync(DefaultRequest<CommentDto> request, CancellationToken ct = default);
    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    Task<Result<DefaultResponse<CommentDto>>> UpdateAsync(DefaultRequest<CommentDto> request, CancellationToken ct = default);
    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
