using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TagRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ITagRepositoryQuery
{
    public async Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default)
        => await DB.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResponse<Tag>> SearchTagsAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default)
    {
        var query = DB.Tags.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(t => t.Name.Contains(request.Filter.SearchTerm));
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(t => t.Name)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<Tag> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
