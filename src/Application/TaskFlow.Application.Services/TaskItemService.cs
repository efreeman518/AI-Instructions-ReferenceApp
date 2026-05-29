using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Messaging;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Contracts.Events;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Model.ValueObjects;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Services;

/// <summary>
/// Service-style TaskItem application boundary. It enforces tenant scope, delegates rules to the
/// aggregate, persists through transaction repositories, and publishes integration events after
/// successful saves without rolling back the saved entity if publishing fails.
/// </summary>
internal class TaskItemService(
    ILogger<TaskItemService> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITaskItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache,
    IIntegrationEventPublisher eventPublisher) : ITaskItemService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    #region Helpers

    /// <summary>Builds response from current configuration and inputs.</summary>
    private static DefaultResponse<TaskItemDto> BuildResponse(TaskItemDto dto) =>
        new() { Item = dto, TenantInfo = null };

    #endregion

    /// <summary>Searches search and returns filtered results for callers.</summary>
    public async Task<PagedResponse<TaskItemDto>> SearchAsync(
        SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            if (request.Filter.TenantId is Guid supplied && supplied != RequestTenantId)
            {
                logger.LogTenantFilterManipulation("TaskItemSearch", RequestTenantId, supplied);
            }
            request.Filter.TenantId = RequestTenantId;
        }
        try
        {
            return await repoQuery.SearchTaskItemsAsync(request, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("TaskItem search cancelled by client.");
            return new PagedResponse<TaskItemDto>();
        }
    }

    /// <summary>Loads requested data and maps missing records to the expected response.</summary>
    public async Task<Result<DefaultResponse<TaskItemDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<TaskItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Get", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TaskItemDto>>.Success(BuildResponse(entity.ToDto()));
    }

    /// <summary>
    /// Creates the aggregate under the caller tenant, saves it first, then publishes the created event.
    /// Event publishing is best-effort because the database save is the source of truth.
    /// </summary>
    public async Task<Result<DefaultResponse<TaskItemDto>>> CreateAsync(
        DefaultRequest<TaskItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, dto.TenantId,
            "TaskItem:Create", nameof(TaskItem));
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId)
            .Bind(e => repoTrxn.UpdateFromDto(e, dto));
        if (entityResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating TaskItem");
            return Result<DefaultResponse<TaskItemDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();

        try
        {
            await eventPublisher.PublishAsync(
                new TaskItemCreatedEvent(entity.Id, entity.TenantId, entity.Title),
                requestContext.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish TaskItemCreatedEvent for {Id}; entity was saved successfully", entity.Id);
        }

        return Result<DefaultResponse<TaskItemDto>>.Success(BuildResponse(resultDto));
    }

    /// <summary>
    /// Updates the aggregate and child collections from one DTO payload. Status transitions run
    /// through the aggregate before the updater syncs children so invalid transitions fail before save.
    /// </summary>
    public async Task<Result<DefaultResponse<TaskItemDto>>> UpdateAsync(
        DefaultRequest<TaskItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;
        dto.TenantId = RequestTenantId ?? Guid.Empty;

        var validation = TaskItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetTaskItemAsync(dto.Id!.Value, ct: ct);
        if (entity == null)
            return Result<DefaultResponse<TaskItemDto>>.Success(new DefaultResponse<TaskItemDto> { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Update", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        var tenantChangeCheck = tenantBoundaryValidator.PreventTenantChange(
            logger, entity.TenantId, dto.TenantId, nameof(TaskItem), entity.Id);
        if (tenantChangeCheck.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(tenantChangeCheck.ErrorMessage!);

        // Handle status transition if changed
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

        // Update value objects
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

        // Sync child collections via Updater - removed items must be hard-deleted,
        // not just unlinked, so the UI's single-payload save can retire checklist
        // items / comments that were removed client-side.
        var syncResult = repoTrxn.UpdateFromDto(entity, dto, RelatedDeleteBehavior.RelationshipAndEntity);
        if (syncResult.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(syncResult.ErrorMessage!);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating TaskItem {Id}", dto.Id);
            return Result<DefaultResponse<TaskItemDto>>.Failure(ex.GetBaseException().Message);
        }

        var resultDto = entity.ToDto();

        if (oldStatus.HasValue)
        {
            try
            {
                await eventPublisher.PublishAsync(
                    new TaskItemStatusChangedEvent(entity.Id, entity.TenantId, oldStatus.Value, entity.Status),
                    requestContext.CorrelationId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to publish TaskItemStatusChangedEvent for {Id}; entity was saved successfully", entity.Id);
            }
        }

        return Result<DefaultResponse<TaskItemDto>>.Success(BuildResponse(resultDto));
    }

    /// <summary>Deletes requested data and maps failures to the caller contract.</summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(id, ct: ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Delete", nameof(TaskItem), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting TaskItem {Id}", id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        await cache.RemoveAsync($"TaskItem:{id}", ct);
        return Result.Success();
    }
}
