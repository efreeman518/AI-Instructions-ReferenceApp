using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates i comment API application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface ICommentApiService
{
    /// <summary>Searches search and returns filtered results for callers.</summary>
    Task<IReadOnlyList<CommentModel>> SearchAsync(Guid? taskItemId = null, CancellationToken ct = default);
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<CommentModel?> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    Task<CommentModel> CreateAsync(CommentModel model, CancellationToken ct = default);
    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    Task<CommentModel> UpdateAsync(CommentModel model, CancellationToken ct = default);
    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
