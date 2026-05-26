using EF.Common.Contracts;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Cqrs.Features.Attachments;

public sealed record SearchAttachmentsQuery(SearchRequest<AttachmentSearchFilter> Request)
    : IQuery<PagedResponse<AttachmentDto>>;

public sealed record GetAttachmentByIdQuery(Guid Id)
    : IQuery<Result<DefaultResponse<AttachmentDto>>>;

public sealed record CreateAttachmentCommand(DefaultRequest<AttachmentDto> Request)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;

public sealed record UploadAttachmentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    AttachmentOwnerType OwnerType,
    Guid OwnerId)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;

public sealed record UpdateAttachmentCommand(DefaultRequest<AttachmentDto> Request)
    : ICommand<Result<DefaultResponse<AttachmentDto>>>;

public sealed record DeleteAttachmentCommand(Guid Id)
    : ICommand<Result>;
