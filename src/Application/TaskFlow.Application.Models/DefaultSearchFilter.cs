namespace TaskFlow.Application.Models;

/// <summary>Provides default search filter behavior for the Application layer.</summary>
public record DefaultSearchFilter
{
    public string? SearchTerm { get; set; }
    public Guid? TenantId { get; set; }
}
