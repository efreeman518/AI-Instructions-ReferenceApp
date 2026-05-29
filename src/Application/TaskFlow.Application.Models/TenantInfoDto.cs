namespace TaskFlow.Application.Models;

/// <summary>Carries tenant info data across API, application, and UI boundaries.</summary>
public record TenantInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
