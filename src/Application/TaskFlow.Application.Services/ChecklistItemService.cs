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

    public async Task<PagedResponse<ChecklistItemDto>> SearchAsync(
        SearchRequest<ChecklistItemSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchChecklistItemsAsync(request, ct);
        return new PagedResponse<ChecklistItemDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetChecklistItemAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<ChecklistItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Get", "ChecklistItem", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<ChecklistItemDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> CreateAsync(
        DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = ChecklistItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "ChecklistItem:Create", "ChecklistItem");
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"ChecklistItem:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<ChecklistItemDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<ChecklistItemDto>>> UpdateAsync(
        DefaultRequest<ChecklistItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = ChecklistItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetChecklistItemAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<ChecklistItemDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Update", "ChecklistItem", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

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

        var resultDto = entity.ToDto();
        await cache.SetAsync($"ChecklistItem:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<ChecklistItemDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetChecklistItemAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "ChecklistItem:Delete", "ChecklistItem", entity.Id);
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
