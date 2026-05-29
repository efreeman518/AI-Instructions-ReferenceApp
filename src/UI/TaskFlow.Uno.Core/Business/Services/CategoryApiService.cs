using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates category API application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public class CategoryApiService(
    TaskFlowApiClient client,
    INotificationService notifications) : ICategoryApiService
{
    /// <summary>Searches search and returns filtered results for callers.</summary>
    public async Task<IReadOnlyList<CategoryModel>> SearchAsync(string? searchTerm = null,
        bool? isActive = null, Guid? parentCategoryId = null, CancellationToken ct = default)
    {
        var response = await client.Api.Categories.Search.PostAsync(new()
        {
            Filter = new() { SearchTerm = searchTerm, IsActive = isActive, ParentCategoryId = parentCategoryId },
            PageNumber = 1,
            PageSize = 100
        }, cancellationToken: ct);

        return response?.Items?.Select(MapToModel).ToList() ?? [];
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<CategoryModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.Categories[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public async Task<CategoryModel> CreateAsync(CategoryModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Categories.PostAsync(dto, cancellationToken: ct);
        var created = MapToModel(result!);
        await notifications.ShowSuccess($"Created category \"{created.Name}\".", ct: ct);
        return created;
    }

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    public async Task<CategoryModel> UpdateAsync(CategoryModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Categories[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        var updated = MapToModel(result!);
        await notifications.ShowSuccess($"Updated category \"{updated.Name}\".", ct: ct);
        return updated;
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.Categories[id].DeleteAsync(cancellationToken: ct);
        await notifications.ShowSuccess("Category deleted.", ct: ct);
    }

    /// <summary>Maps to model into the target contract used by callers.</summary>
    private static CategoryModel MapToModel(CategoryDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name ?? string.Empty, Description = dto.Description,
        SortOrder = dto.SortOrder ?? 0, IsActive = dto.IsActive ?? true,
        ParentCategoryId = dto.ParentCategoryId
    };

    /// <summary>Maps to DTO into the target contract used by callers.</summary>
    private static CategoryDto MapToDto(CategoryModel model) => new()
    {
        Id = model.Id, Name = model.Name, Description = model.Description,
        SortOrder = model.SortOrder, IsActive = model.IsActive,
        ParentCategoryId = model.ParentCategoryId
    };
}
