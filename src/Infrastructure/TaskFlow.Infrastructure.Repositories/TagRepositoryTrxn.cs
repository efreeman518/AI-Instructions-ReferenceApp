using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries tag data through infrastructure storage contracts.</summary>
public class TagRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ITagRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            true,
            filter: (Tag t) => t.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
