using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CommentRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ICommentRepositoryQuery
{
    public async Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            false,
            filter: (Comment c) => c.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<PagedResponse<CommentDto>> SearchCommentsAsync(SearchRequest<CommentSearchFilter> request, CancellationToken ct = default)
    {
        var q = DB.Set<Comment>().ComposeIQueryable(false);

        // ordering
        if (request.Sorts?.Any() ?? false)
        {
            q = q.OrderBy(request.Sorts);
        }
        else
        {
            q = q.OrderBy(e => e.Id);
        }

        // filtering
        var filter = request.Filter;
        if (filter is not null)
        {
            var searchTerm = filter.SearchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(e => e.Body.Contains(searchTerm));

            if (filter.TaskItemId.HasValue)
                q = q.Where(e => e.TaskItemId == filter.TaskItemId.Value);

            if (filter.TenantId.HasValue)
                q = q.Where(e => e.TenantId == filter.TenantId.Value);
        }

        (var data, var total) = await q.QueryPageProjectionAsync(CommentMapper.ProjectorSearch,
            pageSize: request.PageSize, pageIndex: request.PageIndex,
            includeTotal: true, splitQueryOptions: SplitQueryThresholdOptions.Default,
            cancellationToken: ct).ConfigureAwait(ConfigureAwaitOptions.None);

        return new PagedResponse<CommentDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data,
            Total = total
        };
    }
}
