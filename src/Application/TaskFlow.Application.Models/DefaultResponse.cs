namespace TaskFlow.Application.Models;

public record DefaultResponse<T>
{
    public T? Item { get; set; }
    public TenantInfoDto? TenantInfo { get; set; } = null;
}
