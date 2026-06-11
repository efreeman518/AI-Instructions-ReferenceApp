using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Comments;

/// <summary>Provides comment CQRS registrations behavior for the Features Comments layer.</summary>
internal static class CommentCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchCommentsQuery), typeof(PagedResponse<CommentDto>), typeof(SearchCommentsHandler)),
        new(typeof(GetCommentByIdQuery), typeof(Result<DefaultResponse<CommentDto>>), typeof(GetCommentByIdHandler)),
    ];
}
