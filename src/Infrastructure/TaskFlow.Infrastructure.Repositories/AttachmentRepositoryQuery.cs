using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class AttachmentRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), IAttachmentRepositoryQuery
{
    public async Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default)
    {
        return await GetEntityAsync(
            false,
            filter: (Attachment a) => a.Id == id,
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    public async Task<PagedResponse<AttachmentDto>> SearchAttachmentsAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default)
    {
        var q = DB.Set<Attachment>().ComposeIQueryable(false);

        // ordering
        if (request.Sorts?.Any() ?? false)
        {
            q = q.OrderBy(request.Sorts);
        }
        else
        {
            q = q.OrderBy(e => e.FileName);
        }

        // filtering
        var filter = request.Filter;
        if (filter is not null)
        {
            var searchTerm = filter.SearchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(e => e.FileName.Contains(searchTerm));

            if (filter.OwnerType.HasValue)
                q = q.Where(e => e.OwnerType == filter.OwnerType.Value);

            if (filter.OwnerId.HasValue)
                q = q.Where(e => e.OwnerId == filter.OwnerId.Value);

            if (filter.TenantId.HasValue)
                q = q.Where(e => e.TenantId == filter.TenantId.Value);
        }

        (var data, var total) = await q.QueryPageProjectionAsync(AttachmentMapper.ProjectorSearch,
            pageSize: request.PageSize, pageIndex: Math.Max(1, request.PageIndex),
            includeTotal: true, splitQueryOptions: SplitQueryThresholdOptions.Default,
            cancellationToken: ct).ConfigureAwait(ConfigureAwaitOptions.None);

        return new PagedResponse<AttachmentDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data,
            Total = total
        };
    }

    public async Task<int> CountByOwnerAsync(AttachmentOwnerType ownerType, Guid ownerId, CancellationToken ct = default)
        => await DB.Attachments.AsNoTracking().CountAsync(a => a.OwnerType == ownerType && a.OwnerId == ownerId, ct);
}
