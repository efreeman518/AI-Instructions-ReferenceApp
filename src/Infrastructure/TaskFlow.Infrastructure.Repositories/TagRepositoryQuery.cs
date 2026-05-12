using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TagRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ITagRepositoryQuery
{
    public async Task<Tag?> GetTagAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            false,
            filter: (Tag t) => t.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<PagedResponse<TagDto>> SearchTagsAsync(SearchRequest<TagSearchFilter> request, CancellationToken ct = default)
    {
        var q = DB.Set<Tag>().ComposeIQueryable(false);

        // ordering
        if (request.Sorts?.Any() ?? false)
        {
            q = q.OrderBy(request.Sorts);
        }
        else
        {
            q = q.OrderBy(e => e.Name);
        }

        // filtering
        var filter = request.Filter;
        if (filter is not null)
        {
            var searchTerm = filter.SearchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(e => e.Name.Contains(searchTerm));

            if (filter.TenantId.HasValue)
                q = q.Where(e => e.TenantId == filter.TenantId.Value);
        }

        (var data, var total) = await q.QueryPageProjectionAsync(TagMapper.Projection,
            pageSize: request.PageSize, pageIndex: Math.Max(1, request.PageIndex),
            includeTotal: true, splitQueryOptions: SplitQueryThresholdOptions.Default,
            cancellationToken: ct).ConfigureAwait(ConfigureAwaitOptions.None);

        return new PagedResponse<TagDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data,
            Total = total
        };
    }
}
