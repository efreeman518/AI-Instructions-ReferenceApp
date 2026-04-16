namespace TaskFlow.Application.Models;

public record DefaultSearchFilter
{
    public string? SearchTerm { get; set; }
    public Guid? TenantId { get; set; }
}
