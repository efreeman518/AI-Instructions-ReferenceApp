using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Cqrs.Shared;
using TaskFlow.Application.Mappers;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

// Handlers for the TaskItem aggregate's internal children. Every one loads the aggregate root
// (tracked, with children) and mutates through the root's own domain methods, then saves the whole
// graph in one transaction. Children are never created, updated, or deleted through a child
// repository - that would bypass the aggregate's invariants (GR-15). Child removals rely on the
// required Comment/ChecklistItem -> TaskItem relationship: severing it deletes the orphaned row.

/// <summary>Adds a comment to a TaskItem through the aggregate root.</summary>
internal sealed class AddTaskItemCommentHandler(
    ILogger<AddTaskItemCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<AddTaskItemCommentCommand, Result<DefaultResponse<CommentDto>>>
{
    /// <summary>Handles add comment requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(AddTaskItemCommentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return HandlerHelpers.NotFoundResponse<CommentDto>();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:AddComment", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        var addResult = entity.AddComment(command.Comment.Body);
        if (addResult.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(addResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error adding Comment to TaskItem {Id}", ct, command.TaskItemId);
        if (save.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(addResult.Value!.ToDto());
    }
}

/// <summary>Updates a comment owned by a TaskItem through the aggregate root.</summary>
internal sealed class UpdateTaskItemCommentHandler(
    ILogger<UpdateTaskItemCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateTaskItemCommentCommand, Result<DefaultResponse<CommentDto>>>
{
    /// <summary>Handles update comment requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<CommentDto>>> HandleAsync(UpdateTaskItemCommentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return HandlerHelpers.NotFoundResponse<CommentDto>();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:UpdateComment", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(boundary.ErrorMessage!);

        var commentId = DomainId.From<CommentId>(command.CommentId);
        var comment = entity.Comments.FirstOrDefault(c => c.Id == commentId);
        if (comment is null) return HandlerHelpers.NotFoundResponse<CommentDto>();

        var updateResult = comment.Update(command.Comment.Body);
        if (updateResult.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(updateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating Comment {CommentId} on TaskItem {Id}", ct, command.CommentId, command.TaskItemId);
        if (save.IsFailure) return Result<DefaultResponse<CommentDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(comment.ToDto());
    }
}

/// <summary>Removes a comment from a TaskItem through the aggregate root.</summary>
internal sealed class RemoveTaskItemCommentHandler(
    ILogger<RemoveTaskItemCommentHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<RemoveTaskItemCommentCommand, Result>
{
    /// <summary>Handles remove comment requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(RemoveTaskItemCommentCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return Result.Success(); // Idempotent: parent gone means child gone.

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:RemoveComment", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        entity.RemoveComment(DomainId.From<CommentId>(command.CommentId));

        return await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error removing Comment {CommentId} from TaskItem {Id}", ct, command.CommentId, command.TaskItemId);
    }
}

/// <summary>Adds a checklist item to a TaskItem through the aggregate root.</summary>
internal sealed class AddTaskItemChecklistItemHandler(
    ILogger<AddTaskItemChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<AddTaskItemChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles add checklist item requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(AddTaskItemChecklistItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return HandlerHelpers.NotFoundResponse<ChecklistItemDto>();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:AddChecklistItem", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        var addResult = entity.AddChecklistItem(command.ChecklistItem.Title, command.ChecklistItem.SortOrder);
        if (addResult.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(addResult.ErrorMessage!);

        // AddChecklistItem/Create does not take IsCompleted; apply it on the new child so a
        // pre-checked item is not silently dropped.
        if (command.ChecklistItem.IsCompleted) addResult.Value!.Update(isCompleted: true);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error adding ChecklistItem to TaskItem {Id}", ct, command.TaskItemId);
        if (save.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(addResult.Value!.ToDto());
    }
}

/// <summary>Updates a checklist item owned by a TaskItem through the aggregate root.</summary>
internal sealed class UpdateTaskItemChecklistItemHandler(
    ILogger<UpdateTaskItemChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<UpdateTaskItemChecklistItemCommand, Result<DefaultResponse<ChecklistItemDto>>>
{
    /// <summary>Handles update checklist item requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<ChecklistItemDto>>> HandleAsync(UpdateTaskItemChecklistItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return HandlerHelpers.NotFoundResponse<ChecklistItemDto>();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:UpdateChecklistItem", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(boundary.ErrorMessage!);

        var checklistItemId = DomainId.From<ChecklistItemId>(command.ChecklistItemId);
        var item = entity.ChecklistItems.FirstOrDefault(c => c.Id == checklistItemId);
        if (item is null) return HandlerHelpers.NotFoundResponse<ChecklistItemDto>();

        var updateResult = item.Update(command.ChecklistItem.Title, command.ChecklistItem.IsCompleted, command.ChecklistItem.SortOrder);
        if (updateResult.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(updateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error updating ChecklistItem {ChecklistItemId} on TaskItem {Id}", ct, command.ChecklistItemId, command.TaskItemId);
        if (save.IsFailure) return Result<DefaultResponse<ChecklistItemDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(item.ToDto());
    }
}

/// <summary>Removes a checklist item from a TaskItem through the aggregate root.</summary>
internal sealed class RemoveTaskItemChecklistItemHandler(
    ILogger<RemoveTaskItemChecklistItemHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<RemoveTaskItemChecklistItemCommand, Result>
{
    /// <summary>Handles remove checklist item requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(RemoveTaskItemChecklistItemCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return Result.Success(); // Idempotent.

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:RemoveChecklistItem", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        entity.RemoveChecklistItem(DomainId.From<ChecklistItemId>(command.ChecklistItemId));

        return await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error removing ChecklistItem {ChecklistItemId} from TaskItem {Id}", ct, command.ChecklistItemId, command.TaskItemId);
    }
}

/// <summary>Associates an existing Tag with a TaskItem through the aggregate root.</summary>
internal sealed class AssociateTaskItemTagHandler(
    ILogger<AssociateTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<AssociateTaskItemTagCommand, Result<DefaultResponse<TaskItemTagDto>>>
{
    /// <summary>Handles associate tag requests and returns the application result.</summary>
    public async Task<Result<DefaultResponse<TaskItemTagDto>>> HandleAsync(AssociateTaskItemTagCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return HandlerHelpers.NotFoundResponse<TaskItemTagDto>();

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:AssociateTag", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(boundary.ErrorMessage!);

        var associateResult = entity.AssociateTag(DomainId.From<TagId>(command.TagId));
        if (associateResult.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(associateResult.ErrorMessage!);

        var save = await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error associating Tag {TagId} with TaskItem {Id}", ct, command.TagId, command.TaskItemId);
        if (save.IsFailure) return Result<DefaultResponse<TaskItemTagDto>>.Failure(save.ErrorMessage!);

        return HandlerHelpers.Success(associateResult.Value!.ToDto());
    }
}

/// <summary>Removes a Tag association from a TaskItem through the aggregate root.</summary>
internal sealed class RemoveTaskItemTagHandler(
    ILogger<RemoveTaskItemTagHandler> logger,
    IRequestContext<string, Guid?> requestContext,
    ITaskItemRepositoryTrxn repoTrxn,
    ITenantBoundaryValidator tenantBoundaryValidator)
    : IRequestHandler<RemoveTaskItemTagCommand, Result>
{
    /// <summary>Handles remove tag requests and returns the application result.</summary>
    public async Task<Result> HandleAsync(RemoveTaskItemTagCommand command, CancellationToken ct = default)
    {
        var entity = await repoTrxn.GetTaskItemAsync(DomainId.From<TaskItemId>(command.TaskItemId), ct: ct);
        if (entity is null) return Result.Success(); // Idempotent.

        var boundary = tenantBoundaryValidator.EnsureTenantBoundary(
            logger, requestContext.TenantId, requestContext.Roles, entity.TenantId.Value,
            "TaskItem:RemoveTag", nameof(TaskItem), entity.Id.Value);
        if (boundary.IsFailure) return Result.Failure(boundary.ErrorMessage!);

        entity.RemoveTag(DomainId.From<TagId>(command.TagId));

        return await CqrsHandlerSupport.TrySaveAsync(repoTrxn, logger, "Error removing Tag {TagId} from TaskItem {Id}", ct, command.TagId, command.TaskItemId);
    }
}
