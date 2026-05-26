using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

internal static class TaskItemTagCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(GetTaskItemTagByIdQuery), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(GetTaskItemTagByIdHandler)),
        new(typeof(CreateTaskItemTagCommand), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(CreateTaskItemTagHandler)),
        new(typeof(DeleteTaskItemTagCommand), typeof(Result), typeof(DeleteTaskItemTagHandler)),
    ];
}
