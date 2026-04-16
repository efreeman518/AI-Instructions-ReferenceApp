using TaskFlow.Application.Models.Shared;

namespace TaskFlow.Application.Models;

public record TagDto : EntityBaseDto, ITenantEntityDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}
