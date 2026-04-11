using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ChecklistItemRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), IChecklistItemRepositoryTrxn
{
    public async Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default)
        => await db.ChecklistItems.FirstOrDefaultAsync(ci => ci.Id == id, ct);
}
