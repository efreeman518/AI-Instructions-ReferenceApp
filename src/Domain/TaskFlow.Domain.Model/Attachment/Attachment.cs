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

    private Attachment(Guid tenantId, string fileName, string contentType, long fileSizeBytes, string storageUri, AttachmentOwnerType ownerType, Guid ownerId)
    {
        TenantId = tenantId;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        StorageUri = storageUri;
        OwnerType = ownerType;
        OwnerId = ownerId;
    }

    public static DomainResult<Attachment> Create(
        Guid tenantId, string fileName, string contentType,
        long fileSizeBytes, string storageUri,
        AttachmentOwnerType ownerType, Guid ownerId)
    {
        var entity = new Attachment(tenantId, fileName, contentType, fileSizeBytes, storageUri, ownerType, ownerId);
        return entity.Valid();
    }

    public DomainResult<Attachment> Update(string? fileName = null, string? contentType = null, long? fileSizeBytes = null, string? storageUri = null)
    {
        if (fileName is not null) FileName = fileName;
        if (contentType is not null) ContentType = contentType;
        if (fileSizeBytes.HasValue) FileSizeBytes = fileSizeBytes.Value;
        if (storageUri is not null) StorageUri = storageUri;
        return Valid();
    }

    private DomainResult<Attachment> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(FileName)) errors.Add(DomainError.Create("File name is required."));
        if (string.IsNullOrWhiteSpace(ContentType)) errors.Add(DomainError.Create("Content type is required."));
        if (FileSizeBytes <= 0) errors.Add(DomainError.Create("File size must be greater than zero."));
        if (string.IsNullOrWhiteSpace(StorageUri)) errors.Add(DomainError.Create("Storage URI is required."));
        if (OwnerId == Guid.Empty) errors.Add(DomainError.Create("Owner ID cannot be empty."));
        return errors.Count > 0
            ? DomainResult<Attachment>.Failure(errors)
            : DomainResult<Attachment>.Success(this);
    }
}
