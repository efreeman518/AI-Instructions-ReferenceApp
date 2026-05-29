using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

/// <summary>Carries search task items query CQRS data between endpoints and handlers.</summary>
public sealed record SearchTaskItemsQuery(SearchRequest<TaskItemSearchFilter> Request)
    : IQuery<PagedResponse<TaskItemDto>>;

/// <summary>Carries get task item by ID query CQRS data between endpoints and handlers.</summary>
public sealed record GetTaskItemByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TaskItemDto>>>;

/// <summary>Carries create task item command CQRS data between endpoints and handlers.</summary>
public sealed record CreateTaskItemCommand(DefaultRequest<TaskItemDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemDto>>>;

/// <summary>Carries update task item command CQRS data between endpoints and handlers.</summary>
public sealed record UpdateTaskItemCommand(DefaultRequest<TaskItemDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemDto>>>;

/// <summary>Carries delete task item command CQRS data between endpoints and handlers.</summary>
public sealed record DeleteTaskItemCommand(Guid Id)
    : ICommand<Result>;
