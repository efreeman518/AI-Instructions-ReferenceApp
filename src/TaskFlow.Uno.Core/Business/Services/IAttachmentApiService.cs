using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface IAttachmentApiService
{
    Task<IReadOnlyList<AttachmentModel>> SearchAsync(Guid? ownerId = null, string? ownerType = null,
        CancellationToken ct = default);
    Task<AttachmentModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AttachmentModel> CreateAsync(AttachmentModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
