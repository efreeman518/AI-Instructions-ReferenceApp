using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Comments;

/// <summary>Carries search comments query CQRS data between endpoints and handlers.</summary>
public sealed record SearchCommentsQuery(SearchRequest<CommentSearchFilter> Request)
    : IQuery<PagedResponse<CommentDto>>;

/// <summary>Carries get comment by ID query CQRS data between endpoints and handlers.</summary>
public sealed record GetCommentByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<CommentDto>>>;

/// <summary>Carries create comment command CQRS data between endpoints and handlers.</summary>
public sealed record CreateCommentCommand(DefaultRequest<CommentDto> Request)
    : ICommand<Result<DefaultResponse<CommentDto>>>;

/// <summary>Carries update comment command CQRS data between endpoints and handlers.</summary>
public sealed record UpdateCommentCommand(DefaultRequest<CommentDto> Request)
    : ICommand<Result<DefaultResponse<CommentDto>>>;

/// <summary>Carries delete comment command CQRS data between endpoints and handlers.</summary>
public sealed record DeleteCommentCommand(Guid Id)
    : ICommand<Result>;
