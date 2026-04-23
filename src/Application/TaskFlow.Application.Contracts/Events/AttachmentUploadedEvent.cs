using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Events;

public record AttachmentUploadedEvent(
    Guid AttachmentId,
    Guid OwnerId,
    AttachmentOwnerType OwnerType,
    Guid TenantId);