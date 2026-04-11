using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public class TaskItemTag : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }

    // Composite key
    public Guid TaskItemId { get; private set; }
    public Guid TagId { get; private set; }

    // Navigation
    public TaskItem TaskItem { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private TaskItemTag() { }

    public static DomainResult<TaskItemTag> Create(Guid tenantId, Guid taskItemId, Guid tagId)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
