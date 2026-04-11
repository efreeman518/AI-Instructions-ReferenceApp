using TaskFlow.Domain.Model;
using TaskFlow.Domain.Shared.Enums;

namespace Test.Support.Builders;

public class AttachmentBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private string _fileName = "test-file.pdf";
    private string _contentType = "application/pdf";
    private long _fileSizeBytes = 1024;
    private string _storageUri = "https://storage.example.com/test-file.pdf";
    private AttachmentOwnerType _ownerType = AttachmentOwnerType.TaskItem;
    private Guid _ownerId = Guid.NewGuid();

    public AttachmentBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public AttachmentBuilder WithFileName(string fileName) { _fileName = fileName; return this; }
    public AttachmentBuilder WithContentType(string contentType) { _contentType = contentType; return this; }
    public AttachmentBuilder WithFileSizeBytes(long fileSizeBytes) { _fileSizeBytes = fileSizeBytes; return this; }
    public AttachmentBuilder WithStorageUri(string storageUri) { _storageUri = storageUri; return this; }
    public AttachmentBuilder WithOwnerType(AttachmentOwnerType ownerType) { _ownerType = ownerType; return this; }
    public AttachmentBuilder WithOwnerId(Guid ownerId) { _ownerId = ownerId; return this; }

    public Attachment Build()
    {
        var result = Attachment.Create(_tenantId, _fileName, _contentType, _fileSizeBytes, _storageUri, _ownerType, _ownerId);
        return result.Value!;
    }
}
