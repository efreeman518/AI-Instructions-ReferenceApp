using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskItemTagRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ITaskItemTagRepositoryTrxn
{
    public async Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default)
        => await db.TaskItemTags
            .Include(tt => tt.TaskItem)
            .Include(tt => tt.Tag)
            .FirstOrDefaultAsync(tt => tt.Id == id, ct);
}
