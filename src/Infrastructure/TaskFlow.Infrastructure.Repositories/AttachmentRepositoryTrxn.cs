using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries attachment data through infrastructure storage contracts.</summary>
public class AttachmentRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), IAttachmentRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            true,
            filter: (Attachment a) => a.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
