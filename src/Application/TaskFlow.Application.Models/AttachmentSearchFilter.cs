using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Models;

/// <summary>Provides attachment search filter behavior for the Application layer.</summary>
public record AttachmentSearchFilter : DefaultSearchFilter
{
    public AttachmentOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}
