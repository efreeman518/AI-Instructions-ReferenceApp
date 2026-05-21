using System.Linq.Expressions;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskItemTagRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ITaskItemTagRepositoryTrxn
{
    public async Task<TaskItemTag?> GetTaskItemTagAsync(Guid id, CancellationToken ct = default)
    {
        var includesList = new List<Expression<Func<IQueryable<TaskItemTag>, IIncludableQueryable<TaskItemTag, object?>>>>
        {
            q => q.Include(tt => tt.TaskItem),
            q => q.Include(tt => tt.Tag)
        };

        return await GetEntityAsync(
            true,
            filter: (TaskItemTag tt) => tt.Id == id,
            splitQueryThresholdOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList],
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
