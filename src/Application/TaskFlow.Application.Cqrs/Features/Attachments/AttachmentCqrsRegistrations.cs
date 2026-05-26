using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Registration;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.Attachments;

internal static class AttachmentCqrsRegistrations
{
    public static IReadOnlyList<CqrsHandlerRegistration> Registrations { get; } =
    [
        new(typeof(SearchAttachmentsQuery), typeof(PagedResponse<AttachmentDto>), typeof(SearchAttachmentsHandler)),
        new(typeof(GetAttachmentByIdQuery), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(GetAttachmentByIdHandler)),
        new(typeof(CreateAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(CreateAttachmentHandler)),
        new(typeof(UploadAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(UploadAttachmentHandler)),
        new(typeof(UpdateAttachmentCommand), typeof(Result<DefaultResponse<AttachmentDto>>), typeof(UpdateAttachmentHandler)),
        new(typeof(DeleteAttachmentCommand), typeof(Result), typeof(DeleteAttachmentHandler)),
    ];
}
