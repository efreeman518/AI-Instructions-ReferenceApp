using System.Net.Http.Json;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class TaskItemApiService(TaskFlowApiClient client) : ITaskItemApiService
{
    public async Task<IReadOnlyList<TaskItemModel>> SearchAsync(string? searchTerm = null,
        string? status = null, string? priority = null, Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var response = await client.Api.TaskItems.Search.PostAsync(new()
        {
            Filter = new() { SearchTerm = searchTerm, Status = status, Priority = priority, CategoryId = categoryId },
            PageNumber = 1,
            PageSize = 50
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    public async Task<TaskItemModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.TaskItems[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<TaskItemModel> CreateAsync(TaskItemModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.TaskItems.PostAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task<TaskItemModel> UpdateAsync(TaskItemModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.TaskItems[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.TaskItems[id].DeleteAsync(cancellationToken: ct);
    }

    private static TaskItemModel MapToModel(TaskItemDto dto) => new()
    {
        Id = dto.Id, Title = dto.Title ?? string.Empty, Description = dto.Description,
        Priority = dto.Priority ?? "None", Status = dto.Status ?? "Open",
        Features = dto.Features ?? "None",
        EstimatedEffort = dto.EstimatedEffort, ActualEffort = dto.ActualEffort,
        CompletedDate = dto.CompletedDate, CategoryId = dto.CategoryId,
        ParentTaskItemId = dto.ParentTaskItemId,
        StartDate = dto.StartDate, DueDate = dto.DueDate,
        RecurrenceInterval = dto.RecurrenceInterval,
        RecurrenceFrequency = dto.RecurrenceFrequency,
        RecurrenceEndDate = dto.RecurrenceEndDate,
        CategoryName = dto.CategoryName,
        Comments = dto.Comments?.Select(c => new CommentModel
        {
            Id = c.Id, Body = c.Body ?? string.Empty, TaskItemId = c.TaskItemId ?? Guid.Empty
        }).ToList(),
        ChecklistItems = dto.ChecklistItems?.Select(c => new ChecklistItemModel
        {
            Id = c.Id, Title = c.Title ?? string.Empty, IsCompleted = c.IsCompleted ?? false,
            SortOrder = c.SortOrder ?? 0, CompletedDate = c.CompletedDate,
            TaskItemId = c.TaskItemId ?? Guid.Empty
        }).ToList(),
        Tags = dto.Tags?.Select(t => new TagModel
        {
            Id = t.Id, Name = t.Name ?? string.Empty, Color = t.Color
        }).ToList(),
        SubTasks = dto.SubTasks?.Select(MapToModel).ToList()
    };

    private static TaskItemDto MapToDto(TaskItemModel model) => new()
    {
        Id = model.Id, Title = model.Title, Description = model.Description,
        Priority = model.Priority, Status = model.Status, Features = model.Features,
        EstimatedEffort = model.EstimatedEffort, ActualEffort = model.ActualEffort,
        CategoryId = model.CategoryId, ParentTaskItemId = model.ParentTaskItemId,
        StartDate = model.StartDate, DueDate = model.DueDate,
        RecurrenceInterval = model.RecurrenceInterval,
        RecurrenceFrequency = model.RecurrenceFrequency,
        RecurrenceEndDate = model.RecurrenceEndDate
    };
}
