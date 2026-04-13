using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskItemRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ITaskItemRepositoryQuery
{
    public async Task<TaskItem?> GetTaskItemAsync(Guid id, CancellationToken ct = default)
        => await DB.TaskItems
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Comments)
            .Include(t => t.ChecklistItems)
            .Include(t => t.TaskItemTags).ThenInclude(tt => tt.Tag)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResponse<TaskItem>> SearchTaskItemsAsync(SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
    {
        var query = DB.TaskItems.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(t => t.Title.Contains(request.Filter.SearchTerm));
            if (request.Filter.Status.HasValue)
                query = query.Where(t => t.Status == request.Filter.Status.Value);
            if (request.Filter.Priority.HasValue)
                query = query.Where(t => t.Priority == request.Filter.Priority.Value);
            if (request.Filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == request.Filter.CategoryId.Value);
            if (request.Filter.ParentTaskItemId.HasValue)
                query = query.Where(t => t.ParentTaskItemId == request.Filter.ParentTaskItemId.Value);
            if (request.Filter.DueBefore.HasValue)
                query = query.Where(t => t.DateRange.DueDate != null && t.DateRange.DueDate <= request.Filter.DueBefore.Value);
            if (request.Filter.DueAfter.HasValue)
                query = query.Where(t => t.DateRange.DueDate != null && t.DateRange.DueDate >= request.Filter.DueAfter.Value);
            if (request.Filter.IsOverdue == true)
                query = query.Where(t => t.DateRange.DueDate != null && t.DateRange.DueDate < DateTimeOffset.UtcNow && t.CompletedDate == null);
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(t => t.Title)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<TaskItem> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
