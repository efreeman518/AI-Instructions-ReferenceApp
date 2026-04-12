using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface ICommentApiService
{
    Task<IReadOnlyList<CommentModel>> SearchAsync(Guid? taskItemId = null, CancellationToken ct = default);
    Task<CommentModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CommentModel> CreateAsync(CommentModel model, CancellationToken ct = default);
    Task<CommentModel> UpdateAsync(CommentModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
