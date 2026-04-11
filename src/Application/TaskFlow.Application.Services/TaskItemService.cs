using EF.Common.Contracts;
using EF.Data.Contracts;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Application.Services.Rules;
using TaskFlow.Domain.Model.ValueObjects;

namespace TaskFlow.Application.Services;

internal class TaskItemService(
    ILogger<TaskItemService> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITaskItemRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator,
    IEntityCacheProvider cache) : ITaskItemService
{
    private Guid? RequestTenantId => requestContext.TenantId;
    private IReadOnlyCollection<string> RequestRoles => requestContext.Roles;
    private bool IsGlobalAdmin => RequestRoles.Contains(AppConstants.ROLE_GLOBAL_ADMIN);

    public async Task<PagedResponse<TaskItemDto>> SearchAsync(
        SearchRequest<TaskItemSearchFilter> request, CancellationToken ct = default)
    {
        if (!IsGlobalAdmin)
        {
            request.Filter ??= new();
            request.Filter.TenantId = RequestTenantId;
        }
        var page = await repoQuery.SearchTaskItemsAsync(request, ct);
        return new PagedResponse<TaskItemDto>
        {
            Data = page.Data.Select(e => e.ToDto()).ToList(),
            Total = page.Total,
            PageSize = page.PageSize,
            PageIndex = page.PageIndex
        };
    }

    public async Task<Result<DefaultResponse<TaskItemDto>>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemAsync(id, ct);
        if (entity == null) return Result<DefaultResponse<TaskItemDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Get", "TaskItem", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TaskItemDto>>.Success(new() { Item = entity.ToDto() });
    }

    public async Task<Result<DefaultResponse<TaskItemDto>>> CreateAsync(
        DefaultRequest<TaskItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = TaskItemStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, RequestTenantId,
            "TaskItem:Create", "TaskItem");
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(RequestTenantId ?? Guid.Empty);
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
        await cache.SetAsync($"TaskItem:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<TaskItemDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result<DefaultResponse<TaskItemDto>>> UpdateAsync(
        DefaultRequest<TaskItemDto> request, CancellationToken ct = default)
    {
        var dto = request.Item;

        var validation = TaskItemStructureValidator.ValidateUpdate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(validation.Errors);

        var entity = await repoTrxn.GetTaskItemAsync(dto.Id!.Value, ct);
        if (entity == null) return Result<DefaultResponse<TaskItemDto>>.Success(new() { Item = null });

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Update", "TaskItem", entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemDto>>.Failure(boundary.ErrorMessage!);

        // Handle status transition if changed
        if (dto.Status != entity.Status)
        {
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
        await cache.SetAsync($"TaskItem:{entity.Id}", resultDto, ct);
        return Result<DefaultResponse<TaskItemDto>>.Success(new() { Item = resultDto });
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(id, ct);
        if (entity == null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, RequestTenantId, RequestRoles, entity.TenantId,
            "TaskItem:Delete", "TaskItem", entity.Id);
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
