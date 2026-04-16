namespace TaskFlow.Application.Models;

public record TenantInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
