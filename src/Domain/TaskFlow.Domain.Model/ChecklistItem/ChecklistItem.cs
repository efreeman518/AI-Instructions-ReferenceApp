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

    private ChecklistItem(Guid tenantId, Guid taskItemId, string title, int sortOrder)
    {
        TenantId = tenantId;
        TaskItemId = taskItemId;
        Title = title;
        SortOrder = sortOrder;
        IsCompleted = false;
    }

    public static DomainResult<ChecklistItem> Create(Guid tenantId, Guid taskItemId, string title, int sortOrder = 0)
    {
        var entity = new ChecklistItem(tenantId, taskItemId, title, sortOrder);
        return entity.Valid();
    }

    public DomainResult<ChecklistItem> Update(string? title = null, bool? isCompleted = null, int? sortOrder = null)
    {
        if (title is not null) Title = title;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
        if (isCompleted.HasValue)
        {
            IsCompleted = isCompleted.Value;
            CompletedDate = isCompleted.Value ? DateTimeOffset.UtcNow : null;
        }
        return Valid();
    }

    private DomainResult<ChecklistItem> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (TaskItemId == Guid.Empty) errors.Add(DomainError.Create("Task Item ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Title)) errors.Add(DomainError.Create("Title is required."));
        return errors.Count > 0
            ? DomainResult<ChecklistItem>.Failure(errors)
            : DomainResult<ChecklistItem>.Success(this);
    }
}
