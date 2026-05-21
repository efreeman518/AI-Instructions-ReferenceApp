using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class ChecklistItemApiService(
    TaskFlowApiClient client,
    INotificationService notifications) : IChecklistItemApiService
{
    public async Task<IReadOnlyList<ChecklistItemModel>> SearchAsync(Guid? taskItemId = null,
        bool? isCompleted = null, CancellationToken ct = default)
    {
        var response = await client.Api.ChecklistItems.Search.PostAsync(new()
        {
            Filter = new() { TaskItemId = taskItemId, IsCompleted = isCompleted },
            PageNumber = 1,
            PageSize = 100
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    public async Task<ChecklistItemModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.ChecklistItems[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<ChecklistItemModel> CreateAsync(ChecklistItemModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.ChecklistItems.PostAsync(dto, cancellationToken: ct);
        var created = MapToModel(result!);
        await notifications.ShowSuccess($"Added \"{created.Title}\".", ct: ct);
        return created;
    }

    public async Task<ChecklistItemModel> UpdateAsync(ChecklistItemModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.ChecklistItems[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        var updated = MapToModel(result!);
        await notifications.ShowSuccess($"Updated \"{updated.Title}\".", ct: ct);
        return updated;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.ChecklistItems[id].DeleteAsync(cancellationToken: ct);
        await notifications.ShowSuccess("Checklist item deleted.", ct: ct);
    }

    private static ChecklistItemModel MapToModel(ChecklistItemDto dto) => new()
    {
        Id = dto.Id, Title = dto.Title ?? string.Empty, IsCompleted = dto.IsCompleted ?? false,
        SortOrder = dto.SortOrder ?? 0, CompletedDate = dto.CompletedDate,
        TaskItemId = dto.TaskItemId ?? Guid.Empty
    };

    private static ChecklistItemDto MapToDto(ChecklistItemModel model) => new()
    {
        Id = model.Id, Title = model.Title, IsCompleted = model.IsCompleted,
        SortOrder = model.SortOrder, TaskItemId = model.TaskItemId
    };
}
