using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

public class AttachmentSearchFilter
{
    public Guid? TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public AttachmentOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}
