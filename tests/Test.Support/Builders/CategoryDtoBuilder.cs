using TaskFlow.Application.Models;

namespace Test.Support.Builders;

/// <summary>Builds category DTO test data with sensible defaults so tests only override relevant fields.</summary>
public class CategoryDtoBuilder
{
    private Guid? _id = Guid.NewGuid();
    private string _name = "Test Category";
    private string? _description = "Test description";
    private int _sortOrder;
    private bool _isActive = true;
    private Guid? _parentCategoryId;

    /// <summary>Sets ID on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithId(Guid? id) { _id = id; return this; }
    /// <summary>Sets name on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithName(string name) { _name = name; return this; }
    /// <summary>Sets description on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithDescription(string? description) { _description = description; return this; }
    /// <summary>Sets sort order on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }
    /// <summary>Sets is active on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }
    /// <summary>Sets parent category ID on the builder so tests can override only scenario-specific values.</summary>
    public CategoryDtoBuilder WithParentCategoryId(Guid? parentCategoryId) { _parentCategoryId = parentCategoryId; return this; }

    /// <summary>Builds test data used by focused test cases.</summary>
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
