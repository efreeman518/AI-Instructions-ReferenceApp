using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public class Category : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Self-referencing hierarchy
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> SubCategories { get; private set; } = [];

    // Navigation
    public ICollection<TaskItem> TaskItems { get; private set; } = [];

    private Category() { }

    private Category(Guid tenantId, string name, string? description, int sortOrder, Guid? parentCategoryId)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsActive = true;
        ParentCategoryId = parentCategoryId;
    }

    public static DomainResult<Category> Create(Guid tenantId, string name, string? description = null, int sortOrder = 0, Guid? parentCategoryId = null)
    {
        var entity = new Category(tenantId, name, description, sortOrder, parentCategoryId);
        return entity.Valid();
    }

    public DomainResult<Category> Update(string? name = null, string? description = null, int? sortOrder = null, bool? isActive = null, Guid? parentCategoryId = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (parentCategoryId.HasValue) ParentCategoryId = parentCategoryId.Value == Guid.Empty ? null : parentCategoryId.Value;
        return Valid();
    }

    private DomainResult<Category> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Name)) errors.Add(DomainError.Create("Name is required."));
        return errors.Count > 0
            ? DomainResult<Category>.Failure(errors)
            : DomainResult<Category>.Success(this);
    }
}
