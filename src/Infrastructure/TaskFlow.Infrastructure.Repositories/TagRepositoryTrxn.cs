using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TagRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ITagRepositoryTrxn
{
    public async Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default)
        => await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
}
