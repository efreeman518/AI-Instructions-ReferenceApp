namespace TaskFlow.Uno.Core.Business.Models;

public record CategoryModel
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public IReadOnlyList<CategoryModel>? Children { get; init; }
}
