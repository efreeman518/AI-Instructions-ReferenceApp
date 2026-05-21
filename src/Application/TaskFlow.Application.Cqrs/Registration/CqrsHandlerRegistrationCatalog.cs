using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Handlers;
using TaskFlow.Application.Cqrs.Requests;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Registration;

public sealed record CqrsHandlerRegistration(Type RequestType, Type ResponseType, Type HandlerType);

public static class CqrsHandlerRegistrationCatalog
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchCategoriesQuery), typeof(PagedResponse<CategoryDto>), typeof(SearchCategoriesHandler)),
new(typeof(GetCategoryByIdQuery), typeof(Result<DefaultResponse<CategoryDto>>), typeof(GetCategoryByIdHandler)),
new(typeof(CreateCategoryCommand), typeof(Result<DefaultResponse<CategoryDto>>), typeof(CreateCategoryHandler)),
new(typeof(UpdateCategoryCommand), typeof(Result<DefaultResponse<CategoryDto>>), typeof(UpdateCategoryHandler)),
new(typeof(DeleteCategoryCommand), typeof(Result), typeof(DeleteCategoryHandler)),
new(typeof(SearchTagsQuery), typeof(PagedResponse<TagDto>), typeof(SearchTagsHandler)),
new(typeof(GetTagByIdQuery), typeof(Result<DefaultResponse<TagDto>>), typeof(GetTagByIdHandler)),
new(typeof(CreateTagCommand), typeof(Result<DefaultResponse<TagDto>>), typeof(CreateTagHandler)),
new(typeof(UpdateTagCommand), typeof(Result<DefaultResponse<TagDto>>), typeof(UpdateTagHandler)),
new(typeof(DeleteTagCommand), typeof(Result), typeof(DeleteTagHandler)),
new(typeof(SearchTaskItemsQuery), typeof(PagedResponse<TaskItemDto>), typeof(SearchTaskItemsHandler)),
new(typeof(GetTaskItemByIdQuery), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(GetTaskItemByIdHandler)),
new(typeof(CreateTaskItemCommand), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(CreateTaskItemHandler)),
new(typeof(UpdateTaskItemCommand), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(UpdateTaskItemHandler)),
new(typeof(DeleteTaskItemCommand), typeof(Result), typeof(DeleteTaskItemHandler)),
new(typeof(SearchCommentsQuery), typeof(PagedResponse<CommentDto>), typeof(SearchCommentsHandler)),
new(typeof(GetCommentByIdQuery), typeof(Result<DefaultResponse<CommentDto>>), typeof(GetCommentByIdHandler)),
new(typeof(CreateCommentCommand), typeof(Result<DefaultResponse<CommentDto>>), typeof(CreateCommentHandler)),
new(typeof(UpdateCommentCommand), typeof(Result<DefaultResponse<CommentDto>>), typeof(UpdateCommentHandler)),
new(typeof(DeleteCommentCommand), typeof(Result), typeof(DeleteCommentHandler)),
new(typeof(SearchChecklistItemsQuery), typeof(PagedResponse<ChecklistItemDto>), typeof(SearchChecklistItemsHandler)),
new(typeof(GetChecklistItemByIdQuery), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(GetChecklistItemByIdHandler)),
new(typeof(CreateChecklistItemCommand), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(CreateChecklistItemHandler)),
new(typeof(UpdateChecklistItemCommand), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(UpdateChecklistItemHandler)),
new(typeof(DeleteChecklistItemCommand), typeof(Result), typeof(DeleteChecklistItemHandler)),
new(typeof(SearchAttachmentsQuery), typeof(PagedResponse<AttachmentDto>), typeof(SearchAttachmentsHandler)),
new(typeof(GetAttachmentByIdQuery), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(GetAttachmentByIdHandler)),
new(typeof(CreateAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(CreateAttachmentHandler)),
new(typeof(UploadAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(UploadAttachmentHandler)),
new(typeof(UpdateAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(UpdateAttachmentHandler)),
new(typeof(DeleteAttachmentCommand), typeof(Result), typeof(DeleteAttachmentHandler)),
new(typeof(GetTaskItemTagByIdQuery), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(GetTaskItemTagByIdHandler)),
new(typeof(CreateTaskItemTagCommand), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(CreateTaskItemTagHandler)),
new(typeof(DeleteTaskItemTagCommand), typeof(Result), typeof(DeleteTaskItemTagHandler)),
    ];
}
