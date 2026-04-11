using TaskFlow.Domain.Shared;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Domain.Model.Events;

public record AttachmentUploadedEvent(Guid AttachmentId, Guid OwnerId, AttachmentOwnerType OwnerType, Guid TenantId) : IDomainEvent;
