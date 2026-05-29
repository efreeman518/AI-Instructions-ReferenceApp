using TaskFlow.Application.Cqrs.Features.Attachments;
using TaskFlow.Application.Cqrs.Features.Categories;
using TaskFlow.Application.Cqrs.Features.ChecklistItems;
using TaskFlow.Application.Cqrs.Features.Comments;
using TaskFlow.Application.Cqrs.Features.Tags;
using TaskFlow.Application.Cqrs.Features.TaskItems;
using TaskFlow.Application.Cqrs.Features.TaskItemTags;

namespace TaskFlow.Application.Cqrs.Registration;

/// <summary>Provides CQRS handler registration behavior for the Application Registration layer.</summary>
public sealed record CqrsHandlerRegistration(Type RequestType, Type ResponseType, Type HandlerType);

/// <summary>
/// Aggregates handler registrations from feature-owned fragments.
/// DTOs and mappers stay shared in this demo so both application styles keep one API contract.
/// </summary>
public static class CqrsHandlerRegistrationCatalog
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        ..CategoryCqrsRegistrations.Registrations,
        ..TagCqrsRegistrations.Registrations,
        ..TaskItemCqrsRegistrations.Registrations,
        ..CommentCqrsRegistrations.Registrations,
        ..ChecklistItemCqrsRegistrations.Registrations,
        ..AttachmentCqrsRegistrations.Registrations,
        ..TaskItemTagCqrsRegistrations.Registrations,
    ];
}
