namespace TaskFlow.Application.Models.Shared;

/// <summary>Carries i tenant entity data across API, application, and UI boundaries.</summary>
public interface ITenantEntityDto
{
    Guid TenantId { get; set; }
}
