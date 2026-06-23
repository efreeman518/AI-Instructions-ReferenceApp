using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Domain.Shared.Ids;

namespace TaskFlow.Domain.Model;

/// <summary>Models category domain behavior and invariants.</summary>
public class Category : EntityBase<CategoryId>, ITenantEntity<TenantId>
{
    public TenantId TenantId { get; init; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Self-referencing hierarchy
    public CategoryId? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> SubCategories { get; private set; } = [];

    // Navigation
    public ICollection<TaskItem> TaskItems { get; private set; } = [];

    /// <summary>Initializes category with required dependencies and default state.</summary>
    private Category() { }

    /// <summary>Initializes category with required dependencies and default state.</summary>
    private Category(Guid tenantId, string name, string? description, int sortOrder, Guid? parentCategoryId)
    {
        TenantId = TenantId.From(tenantId);
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsActive = true;
        ParentCategoryId = parentCategoryId.HasValue ? CategoryId.From(parentCategoryId.Value) : null;
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public static DomainResult<Category> Create(Guid tenantId, string name, string? description = null, int sortOrder = 0, Guid? parentCategoryId = null)
    {
        var entity = new Category(tenantId, name, description, sortOrder, parentCategoryId);
        return entity.Valid();
    }

    /// <summary>Updates existing data after validation and preserves domain invariants.</summary>
    public DomainResult<Category> Update(string? name = null, string? description = null, int? sortOrder = null, bool? isActive = null, Guid? parentCategoryId = null)
    {
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
        if (parentCategoryId.HasValue) ParentCategoryId = parentCategoryId.Value == Guid.Empty ? null : CategoryId.From(parentCategoryId.Value);
        return Valid();
    }

    /// <summary>Creates a valid category instance with domain-required defaults.</summary>
    private DomainResult<Category> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId.Value == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Name)) errors.Add(DomainError.Create("Name is required."));
        return errors.Count > 0
            ? DomainResult<Category>.Failure(errors)
            : DomainResult<Category>.Success(this);
    }
}
