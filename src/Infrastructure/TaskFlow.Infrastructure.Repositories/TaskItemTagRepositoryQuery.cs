using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskItemTagRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ITaskItemTagRepositoryQuery
{
    public async Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default)
        => await DB.TaskItemTags
            .AsNoTracking()
            .Include(tt => tt.TaskItem)
            .Include(tt => tt.Tag)
            .FirstOrDefaultAsync(tt => tt.Id == id, ct);

    public async Task<List<TaskItemTag>> GetByTaskItemIdAsync(Guid taskItemId, CancellationToken ct = default)
        => await DB.TaskItemTags
            .AsNoTracking()
            .Include(tt => tt.Tag)
            .Where(tt => tt.TaskItemId == taskItemId)
            .ToListAsync(ct);
}
