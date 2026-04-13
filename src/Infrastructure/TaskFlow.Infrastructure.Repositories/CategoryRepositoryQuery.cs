using EF.Common.Contracts;
using EF.Data;
using EF.Data.Contracts;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class CategoryRepositoryQuery(TaskFlowDbContextQuery db)
    : RepositoryBase<TaskFlowDbContextQuery, string, Guid?>(db), ICategoryRepositoryQuery
{
    public async Task<Category?> GetCategoryAsync(Guid id, CancellationToken ct = default)
        => await DB.Categories
            .AsNoTracking()
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResponse<Category>> SearchCategoriesAsync(SearchRequest<CategorySearchFilter> request, CancellationToken ct = default)
    {
        var query = DB.Categories.AsNoTracking().AsQueryable();

        if (request.Filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(request.Filter.SearchTerm))
                query = query.Where(c => c.Name.Contains(request.Filter.SearchTerm));
            if (request.Filter.IsActive.HasValue)
                query = query.Where(c => c.IsActive == request.Filter.IsActive.Value);
            if (request.Filter.ParentCategoryId.HasValue)
                query = query.Where(c => c.ParentCategoryId == request.Filter.ParentCategoryId.Value);
        }

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResponse<Category> { Data = data, Total = total, PageSize = request.PageSize, PageIndex = request.PageIndex };
    }
}
