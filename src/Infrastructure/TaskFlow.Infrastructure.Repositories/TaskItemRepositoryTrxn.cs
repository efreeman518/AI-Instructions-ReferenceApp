using System.Linq.Expressions;
using EF.Data;
using EF.Data.Contracts;
using EF.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Ids;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories.Updaters;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// Transactional TaskItem repository. It returns tracked aggregates for command paths and loads
/// child collections when the application service needs to sync a full task graph.
/// </summary>
public class TaskItemRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ITaskItemRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<TaskItem?> GetTaskItemAsync(Guid id, bool inclChildren = true, CancellationToken ct = default)
    {
        var includesList = new List<Expression<Func<IQueryable<TaskItem>, IIncludableQueryable<TaskItem, object?>>>>
        {
            q => q.Include(t => t.Category)
        };

        if (inclChildren)
        {
            includesList.Add(q => q.Include(t => t.Comments));
            includesList.Add(q => q.Include(t => t.ChecklistItems));
            includesList.Add(q => q.Include(t => t.TaskItemTags).ThenInclude(tt => tt.Tag));
            includesList.Add(q => q.Include(t => t.SubTasks));
        }

        return await GetEntityAsync(
            true,
            filter: t => t.Id == TaskItemId.From(id),
            splitQueryThresholdOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList],
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    /// Delegates DTO graph sync to the DbContext updater so EF change tracking and related deletes
    /// happen inside the same unit of work.
    /// </summary>
    public DomainResult<TaskItem> UpdateFromDto(TaskItem entity, TaskItemDto dto, RelatedDeleteBehavior relatedDeleteBehavior = RelatedDeleteBehavior.None)
        => DB.UpdateFromDto(entity, dto, relatedDeleteBehavior);
}
