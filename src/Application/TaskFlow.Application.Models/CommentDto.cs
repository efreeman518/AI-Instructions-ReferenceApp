using TaskFlow.Application.Models.Shared;

namespace TaskFlow.Application.Models;

public record CommentDto : EntityBaseDto, ITenantEntityDto
{
    public Guid TenantId { get; set; }
    public string Body { get; set; } = null!;
    public Guid TaskItemId { get; set; }
    public List<AttachmentDto>? Attachments { get; set; }
}
