namespace TaskFlow.Application.Models;

/// <summary>Provides default response behavior for the Application layer.</summary>
public record DefaultResponse<T>
{
    public DefaultResponse() { }
    public DefaultResponse(T? item) { Item = item; }

    public T? Item { get; init; }
    public TenantInfoDto? TenantInfo { get; init; }
}
