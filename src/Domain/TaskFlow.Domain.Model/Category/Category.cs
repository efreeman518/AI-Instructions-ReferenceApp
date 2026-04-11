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

    public static DomainResult<Category> Create(Guid tenantId, string name, string? description = null, int sortOrder = 0, Guid? parentCategoryId = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<Category> Update(string? name = null, string? description = null, int? sortOrder = null, bool? isActive = null, Guid? parentCategoryId = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
