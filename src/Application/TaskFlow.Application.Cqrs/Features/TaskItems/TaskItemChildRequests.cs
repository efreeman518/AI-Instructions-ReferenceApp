using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

// Nested sub-resource commands. Comment, ChecklistItem, and the Tag association are internal to
// the TaskItem aggregate, so they have no standalone write handlers. These commands all carry the
// owning TaskItemId and mutate the child through the loaded aggregate root's own domain methods
// (AddComment / RemoveChecklistItem / AssociateTag, ...), never a child repository. See GR-15.

/// <summary>Adds a comment to a TaskItem aggregate through the root.</summary>
public sealed record AddTaskItemCommentCommand(Guid TaskItemId, CommentDto Comment)
    : ICommand<Result<DefaultResponse<CommentDto>>>;

/// <summary>Updates an existing comment owned by a TaskItem aggregate.</summary>
public sealed record UpdateTaskItemCommentCommand(Guid TaskItemId, Guid CommentId, CommentDto Comment)
    : ICommand<Result<DefaultResponse<CommentDto>>>;

/// <summary>Removes a comment from a TaskItem aggregate through the root.</summary>
public sealed record RemoveTaskItemCommentCommand(Guid TaskItemId, Guid CommentId)
    : ICommand<Result>;

/// <summary>Adds a checklist item to a TaskItem aggregate through the root.</summary>
public sealed record AddTaskItemChecklistItemCommand(Guid TaskItemId, ChecklistItemDto ChecklistItem)
    : ICommand<Result<DefaultResponse<ChecklistItemDto>>>;

/// <summary>Updates an existing checklist item owned by a TaskItem aggregate.</summary>
public sealed record UpdateTaskItemChecklistItemCommand(Guid TaskItemId, Guid ChecklistItemId, ChecklistItemDto ChecklistItem)
    : ICommand<Result<DefaultResponse<ChecklistItemDto>>>;

/// <summary>Removes a checklist item from a TaskItem aggregate through the root.</summary>
public sealed record RemoveTaskItemChecklistItemCommand(Guid TaskItemId, Guid ChecklistItemId)
    : ICommand<Result>;

/// <summary>Associates an existing Tag with a TaskItem aggregate through the root.</summary>
public sealed record AssociateTaskItemTagCommand(Guid TaskItemId, Guid TagId)
    : ICommand<Result<DefaultResponse<TaskItemTagDto>>>;

/// <summary>Removes a Tag association from a TaskItem aggregate through the root.</summary>
public sealed record RemoveTaskItemTagCommand(Guid TaskItemId, Guid TagId)
    : ICommand<Result>;
