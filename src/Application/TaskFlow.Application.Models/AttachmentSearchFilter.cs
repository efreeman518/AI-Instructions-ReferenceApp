using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

public class AttachmentSearchFilter
{
    public string? SearchTerm { get; set; }
    public AttachmentOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}
