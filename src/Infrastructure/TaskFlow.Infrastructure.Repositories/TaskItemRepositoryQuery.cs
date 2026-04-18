using System.Linq.Expressions;
using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskItemRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ITaskItemRepositoryQuery
{
    public async Task<TaskItem?> GetTaskItemAsync(Guid id, CancellationToken ct = default)
    {
        var includesList = new List<Expression<Func<IQueryable<TaskItem>, IIncludableQueryable<TaskItem, object?>>>>
        {
            q => q.Include(t => t.Category),
            q => q.Include(t => t.Comments),
            q => q.Include(t => t.ChecklistItems),
            q => q.Include(t => t.TaskItemTags).ThenInclude(tt => tt.Tag),
            q => q.Include(t => t.SubTasks)
        };

        return await GetEntityAsync(
            false,
            filter: t => t.Id == id,
            splitQueryThresholdOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList],
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<PagedResponse<TaskItemDto>> SearchTaskItemsAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
    {
        var q = DB.Set<TaskItem>().ComposeIQueryable(false);

        // ordering
        if (request.Sorts?.Any() ?? false)
        {
            q = q.OrderBy(request.Sorts);
        }
        else
        {
            q = q.OrderBy(e => e.TenantId).ThenBy(e => e.Title);
        }

        // filtering
        var filter = request.Filter;
        if (filter is not null)
        {
            var searchTerm = filter.SearchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(e => e.Title.Contains(searchTerm));

            if (filter.Status.HasValue)
                q = q.Where(e => e.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                q = q.Where(e => e.Priority == filter.Priority.Value);

            if (filter.CategoryId.HasValue)
                q = q.Where(e => e.CategoryId == filter.CategoryId.Value);

            if (filter.ParentTaskItemId.HasValue)
                q = q.Where(e => e.ParentTaskItemId == filter.ParentTaskItemId.Value);

            if (filter.TenantId.HasValue)
                q = q.Where(e => e.TenantId == filter.TenantId.Value);

            if (filter.DueBefore.HasValue)
                q = q.Where(e => e.DateRange.DueDate != null && e.DateRange.DueDate <= filter.DueBefore.Value);

            if (filter.DueAfter.HasValue)
                q = q.Where(e => e.DateRange.DueDate != null && e.DateRange.DueDate >= filter.DueAfter.Value);

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                q = q.Where(e => e.DateRange.DueDate != null && e.DateRange.DueDate < DateTimeOffset.UtcNow && e.CompletedDate == null);
        }

        // includes for SplitQuery
        var includesList = new List<Expression<Func<IQueryable<TaskItem>, IIncludableQueryable<TaskItem, object?>>>>
        {
            q => q.Include(t => t.Category),
            q => q.Include(t => t.TaskItemTags).ThenInclude(tt => tt.Tag)
        };

        (var data, var total) = await q.QueryPageProjectionAsync(TaskItemMapper.ProjectorSearch,
            pageSize: request.PageSize, pageIndex: Math.Max(1, request.PageIndex),
            includeTotal: true, splitQueryOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList], cancellationToken: ct).ConfigureAwait(ConfigureAwaitOptions.None);

        return new PagedResponse<TaskItemDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data,
            Total = total
        };
    }
}
