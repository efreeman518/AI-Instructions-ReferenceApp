using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

/// <summary>Provides task item tag CQRS registrations behavior for the Features Task Item Tags layer.</summary>
internal static class TaskItemTagCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(GetTaskItemTagByIdQuery), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(GetTaskItemTagByIdHandler)),
        new(typeof(CreateTaskItemTagCommand), typeof(Result<DefaultResponse<TaskItemTagDto>>), typeof(CreateTaskItemTagHandler)),
        new(typeof(DeleteTaskItemTagCommand), typeof(Result), typeof(DeleteTaskItemTagHandler)),
    ];
}
