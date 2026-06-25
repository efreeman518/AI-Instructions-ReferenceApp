using System.Linq.Expressions;
using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>Persists and queries category data through infrastructure storage contracts.</summary>
public class CategoryRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ICategoryRepositoryQuery
{
    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var includesList = new List<Expression<Func<IQueryable<Category>, IIncludableQueryable<Category, object?>>>>
        {
            q => q.Include(c => c.SubCategories)
        };

        return await GetEntityAsync(
            false,
            filter: c => c.Id == id,
            splitQueryThresholdOptions: SplitQueryThresholdOptions.Default,
            includes: [.. includesList],
            cancellationToken: ct
        ).ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>Searches search categories and returns filtered results for callers.</summary>
    public async Task<PagedResponse<CategoryDto>> SearchCategoriesAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default)
    {
        var q = DB.Set<Category>().ComposeIQueryable(false);

        // ordering
        if (request.Sorts?.Any() ?? false)
        {
            q = q.OrderBy(request.Sorts);
        }
        else
        {
            q = q.OrderBy(e => e.SortOrder).ThenBy(e => e.Name);
        }

        // filtering
        var filter = request.Filter;
        if (filter is not null)
        {
            var searchTerm = filter.SearchTerm?.Trim();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                q = q.Where(e => e.Name.Contains(searchTerm));

            if (filter.IsActive.HasValue)
            {
                var isActive = filter.IsActive.Value;
                q = q.Where(e => e.IsActive == isActive);
            }

            if (filter.ParentCategoryId.HasValue)
            {
                var parentCategoryId = filter.ParentCategoryId.Value;
                q = q.Where(e => e.ParentCategoryId == parentCategoryId);
            }

            if (filter.TenantId.HasValue)
            {
                var tenantId = filter.TenantId.Value;
                q = q.Where(e => e.TenantId == tenantId);
            }
        }

        (var data, var total) = await q.QueryPageProjectionAsync(CategoryMapper.Projection,
            pageSize: request.PageSize, pageIndex: Math.Max(1, request.PageIndex),
            includeTotal: true, splitQueryOptions: SplitQueryThresholdOptions.Default,
            cancellationToken: ct).ConfigureAwait(ConfigureAwaitOptions.None);

        return new PagedResponse<CategoryDto>
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Data = data,
            Total = total
        };
    }
}
