using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;

namespace TaskFlow.Application.Services;

internal class CategoryService(
    ILogger<CategoryService> logger,
    IRequestContext<string, Guid?> requestContext,
    ICategoryRepositoryTrxn repoTrxn,
    ICategoryRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ICategoryService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<PagedResponse<CategoryDto>> SearchAsync(
        SearchRequest<CategorySearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchCategoriesAsync(request, ct);
        return new PagedResponse<CategoryDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCategoryAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<CategoryDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Get", "Category", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<CategoryDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> CreateAsync(
        DefaultRequest<CategoryDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = CategoryStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "Category:Create", "Category");
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
        if (entityResult.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Category");
            return Result<DefaultResponse<CategoryDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Category:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<CategoryDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> UpdateAsync(
        DefaultRequest<CategoryDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = CategoryStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCategoryAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<CategoryDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Update", "Category", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var updateResult = entity.Update(dto.Name, dto.Description, dto.SortOrder, dto.IsActive, dto.ParentCategoryId);
        if (updateResult.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(updateResult.ErrorMessage!);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Category {Id}", dto.Id);
            return Result<DefaultResponse<CategoryDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();
        await cache.SetAsync($"Category:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<CategoryDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCategoryAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Delete", "Category", entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Category {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync($"Category:{id}", ct);
        return Result.Success();
    }
}
