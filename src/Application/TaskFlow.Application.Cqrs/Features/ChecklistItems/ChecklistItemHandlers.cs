using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Cqrs.Features.ChecklistItems;

/// <summary>Handles search checklist items work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class SearchChecklistItemsHandler(
    ILogger<SearchChecklistItemsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery)
    : IRequestHandler<SearchChecklistItemsQuery, PagedResponse<ChecklistItemDto>>
{
    /// <summary>Handles search checklist items requests and returns the application result.</summary>
    public async Task<PagedResponse<ChecklistItemDto>> HandleAsync(SearchChecklistItemsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "ChecklistItemSearch");
        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchChecklistItemsAsync(request, token), logger, "ChecklistItem", ct);
    }
}

/// <summary>Handles get checklist item by ID work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class GetChecklistItemByIdHandler(
    ILogger<GetChecklistItemByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetChecklistItemByIdQuery, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles get checklist item by ID requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(GetChecklistItemByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetChecklistItemAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<ChecklistItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "ChecklistItem:Get", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles create checklist item work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class CreateChecklistItemHandler(
    ILogger<CreateChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles create checklist item requests and returns the application result.</summary>
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

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating ChecklistItem", ct);
        if (save.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles update checklist item work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class UpdateChecklistItemHandler(
    ILogger<UpdateChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles update checklist item requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(UpdateChecklistItemCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = ChecklistItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetChecklistItemAsync(dto.Id!.Value, ct);
        if (entity is null)
        {
            return HandlerHelpers.NotFoundResponse<ChecklistItemDto>();
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

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating ChecklistItem {Id}", ct, dto.Id);
        if (save.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles delete checklist item work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class DeleteChecklistItemHandler(
    ILogger<DeleteChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    IChecklistItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteChecklistItemCommand, Result>
{
    /// <summary>Handles delete checklist item requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(DeleteChecklistItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetChecklistItemAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "ChecklistItem:Delete", nameof(ChecklistItem), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting ChecklistItem {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        await cache.RemoveAsync(HandlerHelpers.CacheKey(nameof(ChecklistItem), command.Id), ct);
        return Result.Success();
    }
}
