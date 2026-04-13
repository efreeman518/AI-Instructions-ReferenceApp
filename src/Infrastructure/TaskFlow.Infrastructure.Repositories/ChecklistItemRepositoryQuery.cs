using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ChecklistItemRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), IChecklistItemRepositoryQuery
{
    public async Task<ChecklistItem?> GetChecklistItemAsync(Guid id, CancellationToken ct = default)
        => await DB.ChecklistItems.AsNoTracking().FirstOrDefaultAsync(ci => ci.Id == id, ct);

    public async Task<PagedResponse<ChecklistItem>> SearchChecklistItemsAsync(SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default)
    {
        var query = DB.ChecklistItems.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(ci => ci.Title.Contains(request.Filter.SearchTerm));
            if (request.Filter.TaskItemId.HasValue)
                query = query.Where(ci => ci.TaskItemId == request.Filter.TaskItemId.Value);
            if (request.Filter.IsCompleted.HasValue)
                query = query.Where(ci => ci.IsCompleted == request.Filter.IsCompleted.Value);
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(ci => ci.SortOrder)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<ChecklistItem> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
