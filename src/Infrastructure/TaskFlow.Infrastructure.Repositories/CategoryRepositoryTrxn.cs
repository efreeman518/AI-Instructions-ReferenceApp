using System.Linq.Expressions;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries category data through infrastructure storage contracts.</summary>
public class CategoryRepositoryTrxn(TaskFlowDbContextTrxn db)
    : RepositoryTrxn<Category, CategoryId, TaskFlowDbContextTrxn>(db), ICategoryRepositoryTrxn
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Category?> GetCategoryAsync(CategoryId id, CancellationToken ct = default)
    {
        var includesList = new List<Expression<Func<IQueryable<Category>, IIncludableQueryable<Category, object?>>>>
        {
            q => q.Include(c => c.SubCategories)
        };

        return await GetEntityAsync(
            true,
            filter: c => c.Id == id,
            splitQueryThresholdOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList],
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }
}
