using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

public class AttachmentDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _fileName = "test-file.pdf";
    private string _contentType = "application/pdf";
    private long _fileSizeBytes = 1024;
    private string _storageUri = "https://storage.example.com/test-file.pdf";
    private AttachmentOwnerType _ownerType = AttachmentOwnerType.TaskItem;
    private Guid _ownerId = Guid.NewGuid();

    public AttachmentDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public AttachmentDtoBuilder WithFileName(string fileName) { _fileName = fileName; return this; }
    public AttachmentDtoBuilder WithContentType(string contentType) { _contentType = contentType; return this; }
    public AttachmentDtoBuilder WithFileSizeBytes(long fileSizeBytes) { _fileSizeBytes = fileSizeBytes; return this; }
    public AttachmentDtoBuilder WithStorageUri(string storageUri) { _storageUri = storageUri; return this; }
    public AttachmentDtoBuilder WithOwnerType(AttachmentOwnerType ownerType) { _ownerType = ownerType; return this; }
    public AttachmentDtoBuilder WithOwnerId(Guid ownerId) { _ownerId = ownerId; return this; }

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
