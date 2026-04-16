using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

public record AttachmentSearchFilter : DefaultSearchFilter
{
    public AttachmentOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}
