using TaskFlow.Application.Models;

namespace Test.Support.Builders;

public class CategoryDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _name = "Test Category";
    private string? _description = "Test description";
    private int _sortOrder;
    private bool _isActive = true;
    private Guid? _parentCategoryId;

    public CategoryDtoBuilder WithId(Guid? id) { _id = id; return this; }
    public CategoryDtoBuilder WithName(string name) { _name = name; return this; }
    public CategoryDtoBuilder WithDescription(string? description) { _description = description; return this; }
    public CategoryDtoBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }
    public CategoryDtoBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
    public CategoryDtoBuilder WithParentCategoryId(Guid? parentCategoryId) { _parentCategoryId = parentCategoryId; return this; }

    public CategoryDto Build() => new()
    {
        Id = _id,
        Name = _name,
        Description = _description,
        SortOrder = _sortOrder,
        IsActive = _isActive,
        ParentCategoryId = _parentCategoryId
    };
}
