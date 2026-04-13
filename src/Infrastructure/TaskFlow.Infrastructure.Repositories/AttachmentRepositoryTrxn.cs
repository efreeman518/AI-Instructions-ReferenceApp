using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class AttachmentRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), IAttachmentRepositoryTrxn
{
    public async Task<Attachment?> GetAttachmentAsync(Guid id, CancellationToken ct = default)
        => await DB.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);
}
