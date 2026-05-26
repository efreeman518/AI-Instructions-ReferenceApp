using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.ChecklistItems;

internal static class ChecklistItemCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchChecklistItemsQuery), typeof(PagedResponse<ChecklistItemDto>), typeof(SearchChecklistItemsHandler)),
        new(typeof(GetChecklistItemByIdQuery), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(GetChecklistItemByIdHandler)),
        new(typeof(CreateChecklistItemCommand), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(CreateChecklistItemHandler)),
        new(typeof(UpdateChecklistItemCommand), typeof(Result<DefaultResponse<ChecklistItemDto>>), typeof(UpdateChecklistItemHandler)),
        new(typeof(DeleteChecklistItemCommand), typeof(Result), typeof(DeleteChecklistItemHandler)),
    ];
}
