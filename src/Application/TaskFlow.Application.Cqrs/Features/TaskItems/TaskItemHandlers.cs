using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Events;
using TaskFlow.Application.Contracts.Messaging;
using TaskFlow.Application.Contracts.Repositories;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Model.ValueObjects;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

internal sealed class SearchTaskItemsHandler(
    ILogger<SearchTaskItemsHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryQuery repoQuery)
    : IRequestHandler<SearchTaskItemsQuery, PagedResponse<TaskItemDto>>
{
    public async Task<PagedResponse<TaskItemDto>> HandleAsync(SearchTaskItemsQuery query, CancellationToken ct = default)
    {
        var request = query.Request;
        HandlerHelpers.EnforceTenantFilter(request, requestContext.TenantId, requestContext.Roles, logger, "TaskItemSearch");

        return await CqrsHandlerSupport.SearchAsync(token => repoQuery.SearchTaskItemsAsync(request, token), logger, "TaskItem", ct);
    }
}

internal sealed class GetTaskItemByIdHandler(
    ILogger<GetTaskItemByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetTaskItemByIdQuery, Result<DefaultResponse<TaskItemDto>>>
{
    public async Task<Result<DefaultResponse<TaskItemDto>>> HandleAsync(GetTaskItemByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<TaskItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItem:Get", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class CreateTaskItemHandler(
    ILogger<CreateTaskItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<CreateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>>
{
    public async Task<Result<DefaultResponse<TaskItemDto>>> HandleAsync(CreateTaskItemCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "TaskItem:Create", nameof(TaskItem));
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId)
            .Bind(e => repoTrxn.UpdateFromDto(e, dto));
        if (entityResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating TaskItem", ct);
        if (save.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(save.ErrorMessage!);

        await CqrsHandlerSupport.TryPublishAsync(
            eventPublisher,
            new TaskItemCreatedEvent(entity.Id, entity.TenantId, entity.Title),
            requestContext.CorrelationId,
            logger,
            "TaskItem:Create",
            ct);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class UpdateTaskItemHandler(
    ILogger<UpdateTaskItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<UpdateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>>
{
    public async Task<Result<DefaultResponse<TaskItemDto>>> HandleAsync(UpdateTaskItemCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = TaskItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetTaskItemAsync(dto.Id!.Value, ct: ct);
        if (entity is null)
        {
            return HandlerHelpers.NotFoundResponse<TaskItemDto>();
        }

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItem:Update", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(TaskItem), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        TaskItemStatus? oldStatus = null;
        if (dto.Status != entity.Status)
        {
            oldStatus = entity.Status;
            var transitionResult = entity.TransitionStatus(dto.Status);
            if (transitionResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(transitionResult.ErrorMessage!);
        }

        var updateResult = entity.Update(
            dto.Title, dto.Description, dto.Priority, dto.Features,
            dto.EstimatedEffort, dto.ActualEffort, dto.CategoryId, dto.ParentTaskItemId);
        if (updateResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(updateResult.ErrorMessage!);

        entity.UpdateDateRange(dto.StartDate, dto.DueDate);

        if (dto.RecurrenceInterval.HasValue && !string.IsNullOrEmpty(dto.RecurrenceFrequency))
        {
            entity.UpdateRecurrencePattern(new RecurrencePattern
            {
                Interval = dto.RecurrenceInterval.Value,
                Frequency = dto.RecurrenceFrequency!,
                EndDate = dto.RecurrenceEndDate
            });
        }
        else
        {
            entity.UpdateRecurrencePattern(null);
        }

        var syncResult = repoTrxn.UpdateFromDto(entity, dto, RelatedDeleteBehavior.RelationshipAndEntity);
        if (syncResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(syncResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating TaskItem {Id}", ct, dto.Id);
        if (save.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(save.ErrorMessage!);

        if (oldStatus.HasValue)
        {
            await CqrsHandlerSupport.TryPublishAsync(
                eventPublisher,
                new TaskItemStatusChangedEvent(entity.Id, entity.TenantId, oldStatus.Value, entity.Status),
                requestContext.CorrelationId,
                logger,
                "TaskItem:Update",
                ct);
        }

        return HandlerHelpers.Success(entity.ToDto());
    }
}

internal sealed class DeleteTaskItemHandler(
    ILogger<DeleteTaskItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache)
    : IRequestHandler<DeleteTaskItemCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteTaskItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(command.Id, ct: ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItem:Delete", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting TaskItem {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        await cache.RemoveAsync(HandlerHelpers.CacheKey(nameof(TaskItem), command.Id), ct);
        return Result.Success();
    }
}
