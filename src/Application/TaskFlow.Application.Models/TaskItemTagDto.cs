using TaskFlow.Application.Models.Shared;

namespace TaskFlow.Application.Models;

public record TaskItemTagDto : EntityBaseDto, ITenantEntityDto
{
    public Guid TenantId { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid TagId { get; set; }
}
