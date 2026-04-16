using EF.Domain;
using EF.Domain.Contracts;
using TaskFlow.Domain.Model.ValueObjects;
using TaskFlow.Domain.Shared.Constants;
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

    private TaskItem() { }

    private TaskItem(Guid tenantId, string title, string? description, Priority priority, Guid? categoryId, Guid? parentTaskItemId)
    {
        TenantId = tenantId;
        Title = title;
        Description = description;
        Priority = priority;
        Status = TaskItemStatus.Open;
        Features = TaskFeatures.None;
        CategoryId = categoryId;
        ParentTaskItemId = parentTaskItemId;
    }

    public static DomainResult<TaskItem> Create(
        Guid tenantId, string title, string? description = null,
        Priority priority = Priority.None, Guid? categoryId = null,
        Guid? parentTaskItemId = null)
    {
        var entity = new TaskItem(tenantId, title, description, priority, categoryId, parentTaskItemId);
        return entity.Valid();
    }

    public DomainResult<TaskItem> Update(
        string? title = null, string? description = null,
        Priority? priority = null, TaskFeatures? features = null,
        decimal? estimatedEffort = null, decimal? actualEffort = null,
        Guid? categoryId = null, Guid? parentTaskItemId = null)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (priority.HasValue) Priority = priority.Value;
        if (features.HasValue) Features = features.Value;
        if (estimatedEffort.HasValue) EstimatedEffort = estimatedEffort.Value;
        if (actualEffort.HasValue) ActualEffort = actualEffort.Value;
        if (categoryId.HasValue) CategoryId = categoryId.Value == Guid.Empty ? null : categoryId.Value;
        if (parentTaskItemId.HasValue) ParentTaskItemId = parentTaskItemId.Value == Guid.Empty ? null : parentTaskItemId.Value;
        return Valid();
    }

    public DomainResult<TaskItem> TransitionStatus(TaskItemStatus newStatus)
    {
        if (newStatus == TaskItemStatus.None)
        {
            Status = TaskItemStatus.None;
            CompletedDate = null;
            return DomainResult<TaskItem>.Success(this);
        }

        if (!IsValidTransition(Status, newStatus))
            return DomainResult<TaskItem>.Failure($"Cannot transition from {Status} to {newStatus}.");

        var previousStatus = Status;
        Status = newStatus;

        if (newStatus == TaskItemStatus.Completed)
            CompletedDate = DateTimeOffset.UtcNow;
        else if (previousStatus == TaskItemStatus.Completed)
            CompletedDate = null;

        return DomainResult<TaskItem>.Success(this);
    }

    #region Child Collection Methods

    /// <summary>
    /// Add a new comment to this task item.
    /// </summary>
    public DomainResult<Comment> AddComment(string body)
    {
        var result = Comment.Create(TenantId, Id, body);
        if (result.IsFailure) return result;

        Comments.Add(result.Value!);
        return result;
    }

    public DomainResult RemoveComment(Comment comment)
    {
        Comments.Remove(comment);
        return DomainResult.Success();
    }

    public DomainResult RemoveComment(Guid commentId)
    {
        var toRemove = Comments.FirstOrDefault(c => c.Id == commentId);
        if (toRemove != null) Comments.Remove(toRemove);
        return DomainResult.Success(); // Always return success - desired state (comment removed) is achieved
    }

    /// <summary>
    /// Add a new checklist item to this task item.
    /// </summary>
    public DomainResult<ChecklistItem> AddChecklistItem(string title, int sortOrder = 0)
    {
        var result = ChecklistItem.Create(TenantId, Id, title, sortOrder);
        if (result.IsFailure) return result;

        ChecklistItems.Add(result.Value!);
        return result;
    }

    public DomainResult RemoveChecklistItem(ChecklistItem checklistItem)
    {
        ChecklistItems.Remove(checklistItem);
        return DomainResult.Success();
    }

    public DomainResult RemoveChecklistItem(Guid checklistItemId)
    {
        var toRemove = ChecklistItems.FirstOrDefault(ci => ci.Id == checklistItemId);
        if (toRemove != null) ChecklistItems.Remove(toRemove);
        return DomainResult.Success(); // Always return success - desired state is achieved
    }

    /// <summary>
    /// Associate an existing tag with this task item.
    /// </summary>
    public DomainResult<TaskItemTag> AssociateTag(Guid tagId)
    {
        var existing = TaskItemTags.FirstOrDefault(t => t.TagId == tagId);
        if (existing != null) return DomainResult<TaskItemTag>.Success(existing); // Idempotent

        var result = TaskItemTag.Create(TenantId, Id, tagId);
        if (result.IsFailure) return result;

        TaskItemTags.Add(result.Value!);
        return result;
    }

    public DomainResult RemoveTag(TaskItemTag taskItemTag)
    {
        TaskItemTags.Remove(taskItemTag);
        return DomainResult.Success();
    }

    public DomainResult RemoveTag(Guid tagId)
    {
        var toRemove = TaskItemTags.FirstOrDefault(t => t.TagId == tagId);
        if (toRemove != null) TaskItemTags.Remove(toRemove);
        return DomainResult.Success(); // Always return success - desired state (tag not assigned) is achieved
    }

    #endregion

    public void UpdateDateRange(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        DateRange = new DateRange { StartDate = startDate, DueDate = dueDate };
    }

    public void UpdateRecurrencePattern(RecurrencePattern? pattern)
    {
        RecurrencePattern = pattern;
    }

    private static bool IsValidTransition(TaskItemStatus current, TaskItemStatus target) =>
        (current, target) switch
        {
            (TaskItemStatus.Open, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.Open, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.Completed) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.Blocked) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.Blocked, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.Blocked, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.Completed, TaskItemStatus.Open) => true,
            (TaskItemStatus.Cancelled, TaskItemStatus.Open) => true,
            _ => false
        };

    private DomainResult<TaskItem> Valid()
    {
        var errors = new List<DomainError>();
        if (TenantId == Guid.Empty) errors.Add(DomainError.Create("Tenant ID cannot be empty."));
        if (string.IsNullOrWhiteSpace(Title)) errors.Add(DomainError.Create("Title is required."));
        if (Title is not null && Title.Length < DomainConstants.RULE_DEFAULT_NAME_LENGTH_MIN)
            errors.Add(DomainError.Create($"Title must be at least {DomainConstants.RULE_DEFAULT_NAME_LENGTH_MIN} characters."));
        return errors.Count > 0
            ? DomainResult<TaskItem>.Failure(errors)
            : DomainResult<TaskItem>.Success(this);
    }
}
