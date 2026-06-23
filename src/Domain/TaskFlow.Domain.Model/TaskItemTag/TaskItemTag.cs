using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Domain.Shared.Ids;

namespace TaskFlow.Domain.Model;

/// <summary>Models task item tag domain behavior and invariants.</summary>
public class TaskItemTag : EntityBase<TaskItemTagId>, ITenantEntity<TenantId>
{
    public TenantId TenantId { get; init; }

    // Composite key
    public TaskItemId TaskItemId { get; private set; }
    public TagId TagId { get; private set; }

    // Navigation
    public TaskItem TaskItem { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    /// <summary>Initializes task item tag with required dependencies and default state.</summary>
    private TaskItemTag() { }

    /// <summary>Initializes task item tag with required dependencies and default state.</summary>
    private TaskItemTag(Guid tenantId, Guid taskItemId, Guid tagId)
    {
        TenantId = TenantId.From(tenantId);
        TaskItemId = TaskItemId.From(taskItemId);
        TagId = TagId.From(tagId);
    }

    /// <summary>Creates requested data after validation and maps the result to the caller contract.</summary>
    public static DomainResult<TaskItemTag> Create(Guid tenantId, Guid taskItemId, Guid tagId)
    {
        var entity = new TaskItemTag(tenantId, taskItemId, tagId);
        return entity.Valid();
    }

    /// <summary>Creates a valid task item tag instance with domain-required defaults.</summary>
    private DomainResult<TaskItemTag> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId.Value == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (TaskItemId.Value == Guid.Empty) errors.Add(DomainError.Create("Task Item ID cannot be empty."));
        if (TagId.Value == Guid.Empty) errors.Add(DomainError.Create("Tag ID cannot be empty."));
        return errors.Count > 0
            ? DomainResult<TaskItemTag>.Failure(errors)
            : DomainResult<TaskItemTag>.Success(this);
    }
}
