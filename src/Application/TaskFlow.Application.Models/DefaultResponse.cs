namespace TaskFlow.Application.Models;

/// <summary>Provides default response behavior for the Application layer.</summary>
public record DefaultResponse<T>
{
    public T? Item { get; set; }
    public TenantInfoDto? TenantInfo { get; set; } = null;
}
