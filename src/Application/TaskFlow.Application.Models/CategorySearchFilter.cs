namespace TaskFlow.Application.Models;

public record CategorySearchFilter : DefaultSearchFilter
{
    public bool? IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
}
