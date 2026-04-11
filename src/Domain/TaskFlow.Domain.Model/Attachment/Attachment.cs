using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Domain.Model;

public class Attachment : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public string StorageUri { get; private set; } = null!;

    // Polymorphic owner
    public AttachmentOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }

    private Attachment() { }

    public static DomainResult<Attachment> Create(
        Guid tenantId, string fileName, string contentType,
        long fileSizeBytes, string storageUri,
        AttachmentOwnerType ownerType, Guid ownerId)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<Attachment> Update(string? fileName = null, string? contentType = null, long? fileSizeBytes = null, string? storageUri = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
