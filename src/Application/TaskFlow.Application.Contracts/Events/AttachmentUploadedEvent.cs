using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Contracts.Events;

/// <summary>Provides attachment uploaded event behavior for the Application Events layer.</summary>
public record AttachmentUploadedEvent(
    Guid AttachmentId,
    Guid OwnerId,
    AttachmentOwnerType OwnerType,
    Guid TenantId);