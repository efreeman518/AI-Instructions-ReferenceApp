using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface ITagApiService
{
    Task<IReadOnlyList<TagModel>> SearchAsync(string? searchTerm = null, CancellationToken ct = default);
    Task<TagModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<TagModel> CreateAsync(TagModel model, CancellationToken ct = default);
    Task<TagModel> UpdateAsync(TagModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
