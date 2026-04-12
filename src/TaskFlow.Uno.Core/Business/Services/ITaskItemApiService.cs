using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

public interface ITaskItemApiService
{
    Task<IReadOnlyList<TaskItemModel>> SearchAsync(string? searchTerm = null, string? status = null,
        string? priority = null, Guid? categoryId = null, CancellationToken ct = default);
    Task<TaskItemModel?> GetAsync(Guid id, CancellationToken ct = default);
    Task<TaskItemModel> CreateAsync(TaskItemModel model, CancellationToken ct = default);
    Task<TaskItemModel> UpdateAsync(TaskItemModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
