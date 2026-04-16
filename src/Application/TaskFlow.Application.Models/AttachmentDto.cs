using TaskFlow.Application.Models.Shared;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

public record AttachmentDto : EntityBaseDto, ITenantEntityDto
{
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string StorageUri { get; set; } = null!;
    public AttachmentOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
}
