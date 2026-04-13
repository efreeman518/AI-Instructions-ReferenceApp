using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CommentRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ICommentRepositoryTrxn
{
    public async Task<Comment?> GetCommentAsync(Guid id, CancellationToken ct = default)
        => await db.Comments
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
