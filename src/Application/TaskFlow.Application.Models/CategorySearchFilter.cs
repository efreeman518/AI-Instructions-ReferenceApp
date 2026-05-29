namespace TaskFlow.Application.Models;

/// <summary>Provides category search filter behavior for the Application layer.</summary>
public record CategorySearchFilter : DefaultSearchFilter
{
    public bool? IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
}
