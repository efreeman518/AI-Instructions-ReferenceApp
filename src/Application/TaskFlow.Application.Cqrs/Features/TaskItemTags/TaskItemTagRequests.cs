using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

/// <summary>Carries get task item tag by ID query CQRS data between endpoints and handlers.</summary>
public sealed record GetTaskItemTagByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TaskItemTagDto>>>;

/// <summary>Carries create task item tag command CQRS data between endpoints and handlers.</summary>
public sealed record CreateTaskItemTagCommand(DefaultRequest<TaskItemTagDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemTagDto>>>;

/// <summary>Carries delete task item tag command CQRS data between endpoints and handlers.</summary>
public sealed record DeleteTaskItemTagCommand(Guid Id)
    : ICommand<Result>;
