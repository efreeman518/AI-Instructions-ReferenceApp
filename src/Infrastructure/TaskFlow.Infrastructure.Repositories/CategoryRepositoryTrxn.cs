using EF.Data;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CategoryRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryBase<TaskFlowDbContextTrxn, string, Guid?>(db), ICategoryRepositoryTrxn
{
    public async Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default)
        => await db.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
