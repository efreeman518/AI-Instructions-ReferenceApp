using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Domain.Model.ValueObjects;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Domain.Model;

public class TaskItem : EntityBase, ITenantEntity<Guid>
{
    public Guid TenantId { get; init; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskFeatures Features { get; private set; }
    public decimal? EstimatedEffort { get; private set; }
    public decimal? ActualEffort { get; private set; }
    public DateTimeOffset? CompletedDate { get; private set; }

    // Foreign keys
    public Guid? CategoryId { get; private set; }
    public Guid? ParentTaskItemId { get; private set; }

    // Value objects (owned types)
    public DateRange DateRange { get; private set; } = new();
    public RecurrencePattern? RecurrencePattern { get; private set; }

    // Navigation
    public Category? Category { get; private set; }
    public TaskItem? ParentTaskItem { get; private set; }
    public ICollection<TaskItem> SubTasks { get; private set; } = [];
    public ICollection<Comment> Comments { get; private set; } = [];
    public ICollection<ChecklistItem> ChecklistItems { get; private set; } = [];
    public ICollection<TaskItemTag> TaskItemTags { get; private set; } = [];
    public ICollection<Attachment> Attachments { get; private set; } = [];

    private TaskItem() { }

    public static DomainResult<TaskItem> Create(
        Guid tenantId, string title, string? description = null,
        Priority priority = Priority.None, Guid? categoryId = null,
        Guid? parentTaskItemId = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<TaskItem> Update(
        string? title = null, string? description = null,
        Priority? priority = null, TaskFeatures? features = null,
        decimal? estimatedEffort = null, decimal? actualEffort = null,
        Guid? categoryId = null, Guid? parentTaskItemId = null)
        => throw new NotImplementedException("Shell — implement in Phase 5a");

    public DomainResult<TaskItem> TransitionStatus(TaskItemStatus newStatus)
        => throw new NotImplementedException("Shell — implement in Phase 5a");
}
