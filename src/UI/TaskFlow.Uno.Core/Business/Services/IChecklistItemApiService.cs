using TaskFlow.Uno.Core.Business.Models;

namespace TaskFlow.Uno.Core.Business.Services;

/// <summary>Coordinates i checklist item API application use cases with validation, tenant checks, repositories, and response shaping.</summary>
public interface IChecklistItemApiService
{
    /// <summary>Searches search and returns filtered results for callers.</summary>
    Task<IReadOnlyList<ChecklistItemModel>> SearchAsync(Guid? taskItemId = null, bool? isCompleted = null,
        CancellationToken ct = default);
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    Task<ChecklistItemModel?> GetAsync(Guid id, CancellationToken ct = default);
    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    Task<ChecklistItemModel> CreateAsync(ChecklistItemModel model, CancellationToken ct = default);
    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    Task<ChecklistItemModel> UpdateAsync(ChecklistItemModel model, CancellationToken ct = default);
    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
