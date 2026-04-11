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

    private TaskItemTag(Guid tenantId, Guid taskItemId, Guid tagId)
    {
        TenantId = tenantId;
        TaskItemId = taskItemId;
        TagId = tagId;
    }

    public static DomainResult<TaskItemTag> Create(Guid tenantId, Guid taskItemId, Guid tagId)
    {
        var entity = new TaskItemTag(tenantId, taskItemId, tagId);
        return entity.Valid();
    }

    private DomainResult<TaskItemTag> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (TaskItemId == Guid.Empty) errors.Add(DomainError.Create("Task Item ID cannot be empty."));
        if (TagId == Guid.Empty) errors.Add(DomainError.Create("Tag ID cannot be empty."));
        return errors.Count > 0
            ? DomainResult<TaskItemTag>.Failure(errors)
            : DomainResult<TaskItemTag>.Success(this);
    }
}
