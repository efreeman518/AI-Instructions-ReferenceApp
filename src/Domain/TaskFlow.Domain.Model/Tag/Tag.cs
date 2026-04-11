using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public class Tag : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }

    // Navigation
    public ICollection<TaskItemTag> TaskItemTags { get; private set; } = [];

    private Tag() { }

    private Tag(Guid tenantId, string name, string? color)
    {
        TenantId = tenantId;
        Name = name;
        Color = color;
    }

    public static DomainResult<Tag> Create(Guid tenantId, string name, string? color = null)
    {
        var entity = new Tag(tenantId, name, color);
        return entity.Valid();
    }

    public DomainResult<Tag> Update(string? name = null, string? color = null)
    {
        if (name is not null) Name = name;
        if (color is not null) Color = color;
        return Valid();
    }

    private DomainResult<Tag> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Name)) errors.Add(DomainError.Create("Name is required."));
        return errors.Count > 0
            ? DomainResult<Tag>.Failure(errors)
            : DomainResult<Tag>.Success(this);
    }
}
