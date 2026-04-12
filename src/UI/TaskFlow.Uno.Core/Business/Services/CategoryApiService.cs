using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Client;

namespace TaskFlow.Uno.Core.Business.Services;

public class CategoryApiService(TaskFlowApiClient client) : ICategoryApiService
{
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

    public async Task<CategoryModel?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await client.Api.Categories[id].GetAsync(cancellationToken: ct);
        return dto is null ? null : MapToModel(dto);
    }

    public async Task<CategoryModel> CreateAsync(CategoryModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Categories.PostAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task<CategoryModel> UpdateAsync(CategoryModel model, CancellationToken ct = default)
    {
        var dto = MapToDto(model);
        var result = await client.Api.Categories[model.Id!.Value].PutAsync(dto, cancellationToken: ct);
        return MapToModel(result!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await client.Api.Categories[id].DeleteAsync(cancellationToken: ct);
    }

    private static CategoryModel MapToModel(CategoryDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name ?? string.Empty, Description = dto.Description,
        SortOrder = dto.SortOrder ?? 0, IsActive = dto.IsActive ?? true,
        ParentCategoryId = dto.ParentCategoryId
    };

    private static CategoryDto MapToDto(CategoryModel model) => new()
    {
        Id = model.Id, Name = model.Name, Description = model.Description,
        SortOrder = model.SortOrder, IsActive = model.IsActive,
        ParentCategoryId = model.ParentCategoryId
    };
}
