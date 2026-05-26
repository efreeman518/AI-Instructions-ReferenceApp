using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Comments;

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
