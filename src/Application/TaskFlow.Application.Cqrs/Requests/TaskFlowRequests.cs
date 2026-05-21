using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Cqrs.Requests;

public sealed record SearchCategoriesQuery(SearchRequest<CategorySearchFilter> Request)
    : IQuery<PagedResponse<CategoryDto>>;
public sealed record GetCategoryByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<CategoryDto>>>;
public sealed record CreateCategoryCommand(DefaultRequest<CategoryDto> Request)
    : ICommand<Result<DefaultResponse<CategoryDto>>>;
public sealed record UpdateCategoryCommand(DefaultRequest<CategoryDto> Request)
    : ICommand<Result<DefaultResponse<CategoryDto>>>;
public sealed record DeleteCategoryCommand(Guid Id)
    : ICommand<Result>;

public sealed record SearchTagsQuery(SearchRequest<TagSearchFilter> Request)
    : IQuery<PagedResponse<TagDto>>;
public sealed record GetTagByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TagDto>>>;
public sealed record CreateTagCommand(DefaultRequest<TagDto> Request)
    : ICommand<Result<DefaultResponse<TagDto>>>;
public sealed record UpdateTagCommand(DefaultRequest<TagDto> Request)
    : ICommand<Result<DefaultResponse<TagDto>>>;
public sealed record DeleteTagCommand(Guid Id)
    : ICommand<Result>;

public sealed record SearchTaskItemsQuery(SearchRequest<TaskItemSearchFilter> Request)
    : IQuery<PagedResponse<TaskItemDto>>;
public sealed record GetTaskItemByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TaskItemDto>>>;
public sealed record CreateTaskItemCommand(DefaultRequest<TaskItemDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemDto>>>;
public sealed record UpdateTaskItemCommand(DefaultRequest<TaskItemDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemDto>>>;
public sealed record DeleteTaskItemCommand(Guid Id)
    : ICommand<Result>;

public sealed record SearchCommentsQuery(SearchRequest<CommentSearchFilter> Request)
    : IQuery<PagedResponse<CommentDto>>;
public sealed record GetCommentByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<CommentDto>>>;
public sealed record CreateCommentCommand(DefaultRequest<CommentDto> Request)
    : ICommand<Result<DefaultResponse<CommentDto>>>;
public sealed record UpdateCommentCommand(DefaultRequest<CommentDto> Request)
    : ICommand<Result<DefaultResponse<CommentDto>>>;
public sealed record DeleteCommentCommand(Guid Id)
    : ICommand<Result>;

public sealed record SearchChecklistItemsQuery(SearchRequest<ChecklistItemSearchFilter> Request)
    : IQuery<PagedResponse<ChecklistItemDto>>;
public sealed record GetChecklistItemByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<ChecklistItemDto>>>;
public sealed record CreateChecklistItemCommand(DefaultRequest<ChecklistItemDto> Request)
    : ICommand<Result<DefaultResponse<ChecklistItemDto>>>;
public sealed record UpdateChecklistItemCommand(DefaultRequest<ChecklistItemDto> Request)
    : ICommand<Result<DefaultResponse<ChecklistItemDto>>>;
public sealed record DeleteChecklistItemCommand(Guid Id)
    : ICommand<Result>;

public sealed record SearchAttachmentsQuery(SearchRequest<AttachmentSearchFilter> Request)
    : IQuery<PagedResponse<AttachmentDto>>;
public sealed record GetAttachmentByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<AttachmentDto>>>;
public sealed record CreateAttachmentCommand(DefaultRequest<AttachmentDto> Request)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;
public sealed record UploadAttachmentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    AttachmentOwnerType OwnerType,
    Guid OwnerId)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;
public sealed record UpdateAttachmentCommand(DefaultRequest<AttachmentDto> Request)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;
public sealed record DeleteAttachmentCommand(Guid Id)
    : ICommand<Result>;

public sealed record GetTaskItemTagByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TaskItemTagDto>>>;
public sealed record CreateTaskItemTagCommand(DefaultRequest<TaskItemTagDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemTagDto>>>;
public sealed record DeleteTaskItemTagCommand(Guid Id)
    : ICommand<Result>;
