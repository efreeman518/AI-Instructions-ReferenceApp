using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface ICategoryApiService
{
    Task<IReadOnlyList<CategoryModel>> SearchAsync(string? searchTerm = null, bool? isActive = null,
        Guid? parentCategoryId = null, CancellationToken ct = default);
    Task<CategoryModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CategoryModel> CreateAsync(CategoryModel model, CancellationToken ct = default);
    Task<CategoryModel> UpdateAsync(CategoryModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
