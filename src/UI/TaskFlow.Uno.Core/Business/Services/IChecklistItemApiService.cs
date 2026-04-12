using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface IChecklistItemApiService
{
    Task<IReadOnlyList<ChecklistItemModel>> SearchAsync(Guid? taskItemId = null, bool? isCompleted = null,
        CancellationToken ct = default);
    Task<ChecklistItemModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ChecklistItemModel> CreateAsync(ChecklistItemModel model, CancellationToken ct = default);
    Task<ChecklistItemModel> UpdateAsync(ChecklistItemModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
