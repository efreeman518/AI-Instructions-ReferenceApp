using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

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
