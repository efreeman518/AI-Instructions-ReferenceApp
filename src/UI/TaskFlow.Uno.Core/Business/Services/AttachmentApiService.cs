using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class AttachmentApiService(
    TaskFlowApiClient client,
    INotificationService notifications) : IAttachmentApiService
{
    public async Task<IReadOnlyList<AttachmentModel>> SearchAsync(Guid? ownerId = null,
        string? ownerType = null, CancellationToken ct = default)
    {
        var response = await client.Api.Attachments.Search.PostAsync(new()
        {
            Filter = new() { OwnerId = ownerId, OwnerType = ownerType },
            PageNumber = 1,
            PageSize = 100
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    public async Task<AttachmentModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.Attachments[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<AttachmentModel> CreateAsync(AttachmentModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Attachments.PostAsync(dto, cancellationToken: ct);
        var created = MapToModel(result!);
        await notifications.ShowSuccess($"Uploaded {created.FileName}.", ct: ct);
        return created;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.Attachments[id].DeleteAsync(cancellationToken: ct);
        await notifications.ShowSuccess("Attachment deleted.", ct: ct);
    }

    private static AttachmentModel MapToModel(AttachmentDto dto) => new()
    {
        Id = dto.Id, FileName = dto.FileName ?? string.Empty,
        ContentType = dto.ContentType ?? string.Empty,
        FileSizeBytes = dto.FileSizeBytes ?? 0, StorageUri = dto.StorageUri ?? string.Empty,
        OwnerType = dto.OwnerType ?? "TaskItem", OwnerId = dto.OwnerId ?? Guid.Empty
    };

    private static AttachmentDto MapToDto(AttachmentModel model) => new()
    {
        FileName = model.FileName, ContentType = model.ContentType,
        FileSizeBytes = model.FileSizeBytes, StorageUri = model.StorageUri,
        OwnerType = model.OwnerType, OwnerId = model.OwnerId
    };
}
