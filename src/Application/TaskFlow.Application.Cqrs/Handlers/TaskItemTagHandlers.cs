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

internal sealed class GetTaskItemTagByIdHandler(
    ILogger<GetTaskItemTagByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetTaskItemTagByIdQuery, Result<DefaultResponse<TaskItemTagDto>>>
{
    public async Task<Result<DefaultResponse<TaskItemTagDto>>> HandleAsync(GetTaskItemTagByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemTagAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<TaskItemTagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItemTag:Get", nameof(TaskItemTag), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        return Result<DefaultResponse<TaskItemTagDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class CreateTaskItemTagHandler(
    ILogger<CreateTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateTaskItemTagCommand, Result<DefaultResponse<TaskItemTagDto>>>
{
    public async Task<Result<DefaultResponse<TaskItemTagDto>>> HandleAsync(CreateTaskItemTagCommand command, CancellationToken ct = default)
    {
        var dto = command.Request.Item;
        dto.TenantId = requestContext.TenantId ?? Guid.Empty;

        var validation = TaskItemTagStructureValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(validation.Errors);

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, dto.TenantId,
            "TaskItemTag:Create", nameof(TaskItemTag));
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        var entityResult = dto.ToEntity(dto.TenantId);
        if (entityResult.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(entityResult.ErrorMessage!);

        var entity = entityResult.Value!;
        repoTrxn.Create(ref entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating TaskItemTag");
            return Result<DefaultResponse<TaskItemTagDto>>.Failure(ex.GetBaseException().Message);
        }

        return Result<DefaultResponse<TaskItemTagDto>>.Success(HandlerHelpers.BuildResponse(entity.ToDto()));
    }
}

internal sealed class DeleteTaskItemTagHandler(
    ILogger<DeleteTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<DeleteTaskItemTagCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteTaskItemTagCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemTagAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItemTag:Delete", nameof(TaskItemTag), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        try
        {
            await repoTrxn.SaveChangesAsync(OptimisticConcurrencyWinner.ClientWins, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting TaskItemTag {Id}", command.Id);
            return Result.Failure(ex.GetBaseException().Message);
        }

        return Result.Success();
    }
}
