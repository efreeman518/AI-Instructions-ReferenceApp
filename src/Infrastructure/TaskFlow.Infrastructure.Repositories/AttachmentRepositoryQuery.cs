using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class AttachmentRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), IAttachmentRepositoryQuery
{
    public async Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default)
        => await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<PagedResponse<Attachment>> SearchAttachmentsAsync(SearchRequest<AttachmentSearchFilter> request, CancellationToken ct = default)
    {
        var query = db.Attachments.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(a => a.FileName.Contains(request.Filter.SearchTerm));
            if (request.Filter.OwnerType.HasValue)
                query = query.Where(a => a.OwnerType == request.Filter.OwnerType.Value);
            if (request.Filter.OwnerId.HasValue)
                query = query.Where(a => a.OwnerId == request.Filter.OwnerId.Value);
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(a => a.FileName)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<Attachment> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
