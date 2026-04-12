using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class CommentApiService(TaskFlowApiClient client) : ICommentApiService
{
    public async Task<IReadOnlyList<CommentModel>> SearchAsync(Guid? taskItemId = null,
        CancellationToken ct = default)
    {
        var response = await client.Api.Comments.Search.PostAsync(new()
        {
            Filter = new() { TaskItemId = taskItemId },
            PageNumber = 1,
            PageSize = 100
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    public async Task<CommentModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.Comments[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<CommentModel> CreateAsync(CommentModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Comments.PostAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task<CommentModel> UpdateAsync(CommentModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Comments[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.Comments[id].DeleteAsync(cancellationToken: ct);
    }

    private static CommentModel MapToModel(CommentDto dto) => new()
    {
        Id = dto.Id, Body = dto.Body ?? string.Empty, TaskItemId = dto.TaskItemId ?? Guid.Empty,
        Attachments = dto.Attachments?.Select(a => new AttachmentModel
        {
            Id = a.Id, FileName = a.FileName ?? string.Empty, ContentType = a.ContentType ?? string.Empty,
            FileSizeBytes = a.FileSizeBytes ?? 0, StorageUri = a.StorageUri ?? string.Empty,
            OwnerType = a.OwnerType ?? "Comment", OwnerId = a.OwnerId ?? Guid.Empty
        }).ToList()
    };

    private static CommentDto MapToDto(CommentModel model) => new()
    {
        Id = model.Id, Body = model.Body, TaskItemId = model.TaskItemId
    };
}
