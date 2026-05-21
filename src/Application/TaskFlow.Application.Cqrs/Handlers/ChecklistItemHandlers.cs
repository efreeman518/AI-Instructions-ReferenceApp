using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Cqrs.Requests;
using TaskFlow.Application.Cqrs.Validation;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Handlers;

internal sealed class SearchChecklistItemsHandler(
    ILogger<SearchChecklistItemsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery)
    : IRequestHandler<SearchChecklistItemsQuery, PagedResponse<ChecklistItemDto>>
{
    public async Task<PagedResponse<ChecklistItemDto>> HandleAsync(SearchChecklistItemsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "ChecklistItemSearch");
        return await repoQuery.SearchChecklistItemsAsync(request, ct);
    }
}

internal sealed class GetChecklistItemByIdHandler(
    ILogger<GetChecklistItemByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetChecklistItemByIdQuery, Result<DefaultResponse<ChecklistItemDto>>>
{
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(GetChecklistItemByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetChecklistItemAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<ChecklistItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "ChecklistItem:Get", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<ChecklistItemDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class CreateChecklistItemHandler(
    ILogger<CreateChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(CreateChecklistItemCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = ChecklistItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
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

        return Result<DefaultResponse<ChecklistItemDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class UpdateChecklistItemHandler(
    ILogger<UpdateChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(UpdateChecklistItemCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = ChecklistItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetChecklistItemAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return Result<DefaultResponse<ChecklistItemDto>>.Success(HandlerHelpers.BuildResponse<ChecklistItemDto>(null));
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
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

        return Result<DefaultResponse<ChecklistItemDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class DeleteChecklistItemHandler(
    ILogger<DeleteChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteChecklistItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetChecklistItemAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "ChecklistItem:Delete", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting ChecklistItem {Id}", command.Id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync("ChecklistItem:" + command.Id, ct);
        return Result.Success();
    }
}
