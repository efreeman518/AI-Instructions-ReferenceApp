using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.ChecklistItems;

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
