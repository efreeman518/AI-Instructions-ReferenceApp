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
        const int pageSize = 100;

        var firstPage = await SearchPageAsync(searchTerm, status, priority, categoryId, 1, pageSize, ct);
        if (firstPage.TotalCount <= firstPage.Items.Count)
        {
            return firstPage.Items;
        }

        var allItems = firstPage.Items.ToList();
        for (var pageNumber = 2; pageNumber <= firstPage.TotalPages; pageNumber++)
        {
            var nextPage = await SearchPageAsync(searchTerm, status, priority, categoryId, pageNumber, pageSize, ct);
            allItems.AddRange(nextPage.Items);
        }

        return allItems;
    }

    public async Task<TaskItemSearchPage> SearchPageAsync(string? searchTerm = null,
        string? status = null, string? priority = null, Guid? categoryId = null,
        int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Max(1, pageSize);

        var response = await SearchCoreAsync(searchTerm, status, priority, categoryId, normalizedPageNumber, normalizedPageSize, ct)
            ?? new PagedResponse<TaskItemDto>();

        return new TaskItemSearchPage
        {
            Items = response.Items?.Select(MapToModel).ToList() ?? [],
            TotalCount = response.TotalCount,
            PageNumber = response.PageNumber > 0 ? response.PageNumber : normalizedPageNumber,
            PageSize = response.PageSize > 0 ? response.PageSize : normalizedPageSize
        };
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

    private async Task<PagedResponse<TaskItemDto>?> SearchCoreAsync(string? searchTerm,
        string? status, string? priority, Guid? categoryId, int pageNumber,
        int pageSize, CancellationToken ct)
    {
        var normalizedSearchTerm = NormalizeOptionalFilter(searchTerm);
        var normalizedStatus = NormalizeOptionalFilter(status, "All", "Any");
        var normalizedPriority = NormalizeOptionalFilter(priority, "All", "Any");

        return await client.Api.TaskItems.Search.PostAsync(new()
        {
            Filter = new()
            {
                SearchTerm = normalizedSearchTerm,
                Status = normalizedStatus,
                Priority = normalizedPriority,
                CategoryId = categoryId
            },
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken: ct);
    }

    private static string? NormalizeOptionalFilter(string? value, params string[] emptyAliases)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return emptyAliases.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase))
            ? null
            : normalized;
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
        RecurrenceEndDate = model.RecurrenceEndDate,
        // Children sent in the same payload so create/update is a single
        // atomic server call — avoids the split "create task + update child"
        // flow where the child update could lose fields (e.g. IsCompleted).
        ChecklistItems = model.ChecklistItems?.Select(c => new ChecklistItemDto
        {
            Id = c.Id, Title = c.Title, IsCompleted = c.IsCompleted,
            SortOrder = c.SortOrder, CompletedDate = c.CompletedDate,
            TaskItemId = c.TaskItemId == Guid.Empty ? null : c.TaskItemId
        }).ToList(),
        Comments = model.Comments?.Select(c => new CommentDto
        {
            Id = c.Id, Body = c.Body,
            TaskItemId = c.TaskItemId == Guid.Empty ? null : c.TaskItemId
        }).ToList()
    };
}
