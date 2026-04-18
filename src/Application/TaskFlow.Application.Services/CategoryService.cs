using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;
using TaskFlow.Domain.Model;

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

    #region Helpers

    private static DefaultResponse<CategoryDto> BuildResponse(CategoryDto dto) =>
        new() { Item = dto, TenantInfo = null };

    #endregion

    public async Task<PagedResponse<CategoryDto>> SearchAsync(
        SearchRequest<CategorySearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            if (request.Filter.TenantId is Guid supplied && supplied != RequestTenantId)
            {
                logger.LogTenantFilterManipulation("CategorySearch", RequestTenantId, supplied);
            }
            request.Filter.TenantId = RequestTenantId;
        }
        return await repoQuery.SearchCategoriesAsync(request, ct);
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetCategoryAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<CategoryDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Get", nameof(Category), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<CategoryDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> CreateAsync(
        DefaultRequest<CategoryDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = CategoryStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, dto.TenantId,
            "Category:Create", nameof(Category));
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
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

        return Result<DefaultResponse<CategoryDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result<DefaultResponse<CategoryDto>>> UpdateAsync(
        DefaultRequest<CategoryDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = CategoryStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetCategoryAsync(dto.Id!.Value, ct);
        if (entity == null)
            return Result<DefaultResponse<CategoryDto>>.Success(new DefaultResponse<CategoryDto> { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Update", nameof(Category), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(Category), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<CategoryDto>>.Failure(tenantChangeCheck.ErrorMessage!);

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

        return Result<DefaultResponse<CategoryDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetCategoryAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "Category:Delete", nameof(Category), entity.Id);
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
