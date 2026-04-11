using TaskFlow.Domain.Model;

namespace Test.Support.Builders;

public class CategoryBuilder
{
    private Guid _tenantId = TestConstants.TenantId;
    private string _name = "Test Category";
    private string? _description = "Test description";
    private int _sortOrder;
    private Guid? _parentCategoryId;

    public CategoryBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public CategoryBuilder WithName(string name) { _name = name; return this; }
    public CategoryBuilder WithDescription(string? description) { _description = description; return this; }
    public CategoryBuilder WithSortOrder(int sortOrder) { _sortOrder = sortOrder; return this; }
    public CategoryBuilder WithParentCategoryId(Guid? parentCategoryId) { _parentCategoryId = parentCategoryId; return this; }

    public Category Build()
    {
        var result = Category.Create(_tenantId, _name, _description, _sortOrder, _parentCategoryId);
        return result.Value!;
    }
}
