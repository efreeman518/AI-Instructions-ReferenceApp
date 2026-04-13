using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CommentRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ICommentRepositoryQuery
{
    public async Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default)
        => await db.Comments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResponse<Comment>> SearchCommentsAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default)
    {
        var query = db.Comments.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(c => c.Body.Contains(request.Filter.SearchTerm));
            if (request.Filter.TaskItemId.HasValue)
                query = query.Where(c => c.TaskItemId == request.Filter.TaskItemId.Value);
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(c => c.Id)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<Comment> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
