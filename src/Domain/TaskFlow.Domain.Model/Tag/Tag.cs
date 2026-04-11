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

    public static DomainResult<Tag> Create(Guid tenantId, string name, string? color = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<Tag> Update(string? name = null, string? color = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
