using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

public sealed record GetTaskItemTagByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<TaskItemTagDto>>>;

public sealed record CreateTaskItemTagCommand(DefaultRequest<TaskItemTagDto> Request)
    : ICommand<Result<DefaultResponse<TaskItemTagDto>>>;

public sealed record DeleteTaskItemTagCommand(Guid Id)
    : ICommand<Result>;
