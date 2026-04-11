namespace TaskFlow.Application.Models;

public class TagSearchFilter
{
    public Guid? TenantId { get; set; }
    public string? SearchTerm { get; set; }
}
