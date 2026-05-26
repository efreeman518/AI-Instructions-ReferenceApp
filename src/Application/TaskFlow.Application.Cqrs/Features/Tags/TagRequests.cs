using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Tags;

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
