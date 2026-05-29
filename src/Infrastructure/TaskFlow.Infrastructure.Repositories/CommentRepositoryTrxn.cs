using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries comment data through infrastructure storage contracts.</summary>
public class CommentRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ICommentRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            true,
            filter: (Comment c) => c.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
