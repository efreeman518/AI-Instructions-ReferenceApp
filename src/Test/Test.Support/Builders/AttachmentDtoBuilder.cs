using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

/// <summary>Builds attachment DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class AttachmentDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _fileName = "test-file.pdf";
    private string _contentType = "application/pdf";
    private long _fileSizeBytes = 1024;
    private string _storageUri = "https://storage.example.com/test-file.pdf";
    private AttachmentOwnerType _ownerType = AttachmentOwnerType.TaskItem;
    private Guid _ownerId = Guid.NewGuid();

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets file name on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithFileName(string fileName) { _fileName = fileName; return this; }
    /// <summary>Sets content type on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithContentType(string contentType) { _contentType = contentType; return this; }
    /// <summary>Sets file size bytes on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithFileSizeBytes(long fileSizeBytes) { _fileSizeBytes = fileSizeBytes; return this; }
    /// <summary>Sets storage uri on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithStorageUri(string storageUri) { _storageUri = storageUri; return this; }
    /// <summary>Sets owner type on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithOwnerType(AttachmentOwnerType ownerType) { _ownerType = ownerType; return this; }
    /// <summary>Sets owner ID on the builder so tests can override only scenario-specific values.</summary>
    public AttachmentDtoBuilder WithOwnerId(Guid ownerId) { _ownerId = ownerId; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
    public AttachmentDto Build() => new()
    {
        Id = _id,
        FileName = _fileName,
        ContentType = _contentType,
        FileSizeBytes = _fileSizeBytes,
        StorageUri = _storageUri,
        OwnerType = _ownerType,
        OwnerId = _ownerId
    };
}
