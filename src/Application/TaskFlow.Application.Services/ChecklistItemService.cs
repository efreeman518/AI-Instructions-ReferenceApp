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

internal class ChecklistItemService(
    ILogger<ChecklistItemService> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    IChecklistItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : IChecklistItemService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    #region Helpers

    private static DefaultResponse<ChecklistItemDto> BuildResponse(ChecklistItemDto dto) =>
        new() { Item = dto, TenantInfo = null };

    #endregion

    public async Task<PagedResponse<ChecklistItemDto>> SearchAsync(
        SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            if (request.Filter.TenantId is Guid supplied && supplied != RequestTenantId)
            {
                logger.LogTenantFilterManipulation("ChecklistItemSearch", RequestTenantId, supplied);
            }
            request.Filter.TenantId = RequestTenantId;
        }
        return await repoQuery.SearchChecklistItemsAsync(request, ct);
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetChecklistItemAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<ChecklistItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Get", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<ChecklistItemDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> CreateAsync(
        DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = ChecklistItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, dto.TenantId,
            "ChecklistItem:Create", nameof(ChecklistItem));
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ChecklistItem");
            return Result<DefaultResponse<ChecklistItemDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<ChecklistItemDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> UpdateAsync(
        DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = ChecklistItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetChecklistItemAsync(dto.Id!.Value, ct);
        if (entity == null)
            return Result<DefaultResponse<ChecklistItemDto>>.Failure($"{ErrorConstants.ERROR_ITEM_NOTFOUND}: {dto.Id}");

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Update", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(ChecklistItem), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        var updateResult = entity.Update(dto.Title, dto.IsCompleted, dto.SortOrder);
        if (updateResult.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(updateResult.ErrorMessage!);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating ChecklistItem {Id}", dto.Id);
            return Result<DefaultResponse<ChecklistItemDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<ChecklistItemDto>>.Success(BuildResponse(entity.ToDto()));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetChecklistItemAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Delete", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting ChecklistItem {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync($"ChecklistItem:{id}", ct);
        return Result.Success();
    }
}
