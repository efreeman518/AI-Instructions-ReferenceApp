using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Comments;

internal static class CommentCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchCommentsQuery), typeof(PagedResponse<CommentDto>), typeof(SearchCommentsHandler)),
        new(typeof(GetCommentByIdQuery), typeof(Result<DefaultResponse<CommentDto>>), typeof(GetCommentByIdHandler)),
        new(typeof(CreateCommentCommand), typeof(Result<DefaultResponse<CommentDto>>), typeof(CreateCommentHandler)),
        new(typeof(UpdateCommentCommand), typeof(Result<DefaultResponse<CommentDto>>), typeof(UpdateCommentHandler)),
        new(typeof(DeleteCommentCommand), typeof(Result), typeof(DeleteCommentHandler)),
    ];
}
