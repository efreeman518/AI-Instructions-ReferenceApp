using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItems;

/// <summary>Provides task item CQRS registrations behavior for the Features Task Items layer.</summary>
internal static class TaskItemCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchTaskItemsQuery), typeof(PagedResponse<TaskItemDto>), typeof(SearchTaskItemsHandler)),
        new(typeof(GetTaskItemByIdQuery), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(GetTaskItemByIdHandler)),
        new(typeof(CreateTaskItemCommand), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(CreateTaskItemHandler)),
        new(typeof(UpdateTaskItemCommand), typeof(Result<DefaultResponse<TaskItemDto>>), typeof(UpdateTaskItemHandler)),
        new(typeof(DeleteTaskItemCommand), typeof(Result), typeof(DeleteTaskItemHandler)),
    ];
}
