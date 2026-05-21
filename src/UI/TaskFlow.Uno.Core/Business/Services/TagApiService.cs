using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class TagApiService(
    TaskFlowApiClient client,
    INotificationService notifications) : ITagApiService
{
    public async Task<IReadOnlyList<TagModel>> SearchAsync(string? searchTerm = null,
        CancellationToken ct = default)
    {
        var response = await client.Api.Tags.Search.PostAsync(new()
        {
            Filter = new() { SearchTerm = searchTerm },
            PageNumber = 1,
            PageSize = 100
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    public async Task<TagModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.Tags[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<TagModel> CreateAsync(TagModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Tags.PostAsync(dto, cancellationToken: ct);
        var created = MapToModel(result!);
        await notifications.ShowSuccess($"Created tag \"{created.Name}\".", ct: ct);
        return created;
    }

    public async Task<TagModel> UpdateAsync(TagModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Tags[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        var updated = MapToModel(result!);
        await notifications.ShowSuccess($"Updated tag \"{updated.Name}\".", ct: ct);
        return updated;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.Tags[id].DeleteAsync(cancellationToken: ct);
        await notifications.ShowSuccess("Tag deleted.", ct: ct);
    }

    private static TagModel MapToModel(TagDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name ?? string.Empty, Color = dto.Color
    };

    private static TagDto MapToDto(TagModel model) => new()
    {
        Id = model.Id, Name = model.Name, Color = model.Color
    };
}
