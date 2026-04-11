using EF.Domain;
using EF.Domain.Contracts;

namespace TaskFlow.Domain.Model;

public class ChecklistItem : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string Title { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset? CompletedDate { get; private set; }

    // Foreign key
    public Guid TaskItemId { get; private set; }

    // Navigation
    public TaskItem TaskItem { get; private set; } = null!;

    private ChecklistItem() { }

    public static DomainResult<ChecklistItem> Create(Guid tenantId, Guid taskItemId, string title, int sortOrder = 0)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<ChecklistItem> Update(string? title = null, bool? isCompleted = null, int? sortOrder = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
