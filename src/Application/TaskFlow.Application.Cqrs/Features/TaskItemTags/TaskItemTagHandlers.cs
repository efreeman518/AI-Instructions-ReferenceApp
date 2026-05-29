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

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

/// <summary>Handles get task item tag by ID work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class GetTaskItemTagByIdHandler(
    ILogger<GetTaskItemTagByIdHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryQuery repoQuery,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<GetTaskItemTagByIdQuery, Result<DefaultResponse<TaskItemTagDto>>>
{
    /// <summary>Handles get task item tag by ID requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<TaskItemTagDto>>> HandleAsync(GetTaskItemTagByIdQuery query, CancellationToken ct = default)
    {
        var entity = await repoQuery.GetTaskItemTagAsync(query.Id, ct);
        if (entity is null) return Result<DefaultResponse<TaskItemTagDto>>.None();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItemTag:Get", nameof(TaskItemTag), entity.Id);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles create task item tag work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class CreateTaskItemTagHandler(
    ILogger<CreateTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<CreateTaskItemTagCommand, Result<DefaultResponse<TaskItemTagDto>>>
{
    /// <summary>Handles create task item tag requests and returns the application result.</summary>
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

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error creating TaskItemTag", ct);
        if (save.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(entity.ToDto());
    }
}

/// <summary>Handles delete task item tag work by coordinating validation, tenant boundaries, persistence, and response mapping.</summary>
internal sealed class DeleteTaskItemTagHandler(
    ILogger<DeleteTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemTagRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<DeleteTaskItemTagCommand, Result>
{
    /// <summary>Handles delete task item tag requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(DeleteTaskItemTagCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemTagAsync(command.Id, ct);
        if (entity is null) return Result.Success();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId,
            "TaskItemTag:Delete", nameof(TaskItemTag), entity.Id);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        repoTrxn.Delete(entity);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error deleting TaskItemTag {Id}", ct, command.Id);
        if (save.IsFailure) return save;

        return Result.Success();
    }
}
