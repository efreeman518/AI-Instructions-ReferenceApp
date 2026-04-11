namespace TaskFlow.Application.Models;

public class CategorySearchFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public Guid? ParentCategoryId { get; set; }
}
