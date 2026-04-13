using Microsoft.Extensions.Logging;
using TaskFlow.Application.Contracts.Repositories;
using TaskFlow.Application.Contracts.Services;
using TaskFlow.Application.Contracts.Storage;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Services;

public class TaskViewProjectionService : ITaskViewProjectionService
{
    private readonly ITaskItemRepositoryQuery _taskItemRepo;
    private readonly IAttachmentRepositoryQuery _attachmentRepo;
    private readonly ITaskViewRepository _taskViewRepo;
    private readonly ILogger<TaskViewProjectionService> _logger;

    public TaskViewProjectionService(
        ITaskItemRepositoryQuery taskItemRepo,
        IAttachmentRepositoryQuery attachmentRepo,
        ITaskViewRepository taskViewRepo,
        ILogger<TaskViewProjectionService> logger)
    {
        _taskItemRepo = taskItemRepo;
        _attachmentRepo = attachmentRepo;
        _taskViewRepo = taskViewRepo;
        _logger = logger;
    }

    public async Task ProjectTaskItemAsync(Guid taskItemId, CancellationToken ct = default)
    {
        var entity = await _taskItemRepo.GetTaskItemAsync(taskItemId, ct);
        if (entity is null)
        {
            _logger.LogWarning("TaskItem {Id} not found for projection", taskItemId);
            return;
        }

        var taskView = new TaskViewDto
        {
            Id = entity.Id.ToString(),
            TenantId = entity.TenantId.ToString(),
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status.ToString(),
            Priority = entity.Priority.ToString(),
            CategoryName = entity.Category?.Name,
            StartDate = entity.DateRange.StartDate,
            DueDate = entity.DateRange.DueDate,
            CompletedDate = entity.CompletedDate,
            IsOverdue = entity.DateRange.DueDate.HasValue
                        && entity.DateRange.DueDate < DateTimeOffset.UtcNow
                        && entity.CompletedDate is null,
            Tags = entity.TaskItemTags.Select(tt => tt.Tag?.Name ?? "").Where(n => n.Length > 0).ToList(),
            CommentCount = entity.Comments.Count,
            ChecklistTotal = entity.ChecklistItems.Count,
            ChecklistCompleted = entity.ChecklistItems.Count(ci => ci.IsCompleted),
            AttachmentCount = await _attachmentRepo.CountByOwnerAsync(AttachmentOwnerType.TaskItem, entity.Id, ct),
            SubTaskCount = entity.SubTasks.Count,
            LastModifiedUtc = DateTimeOffset.UtcNow,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        await _taskViewRepo.UpsertAsync(taskView, ct);
        _logger.LogInformation("Projected TaskItem {Id} to TaskView", taskItemId);
    }
}
