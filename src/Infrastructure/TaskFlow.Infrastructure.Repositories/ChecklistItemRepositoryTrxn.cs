using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries checklist item data through infrastructure storage contracts.</summary>
public class ChecklistItemRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), IChecklistItemRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            true,
            filter: (ChecklistItem ci) => ci.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
