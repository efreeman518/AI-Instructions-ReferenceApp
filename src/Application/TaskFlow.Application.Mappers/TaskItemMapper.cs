using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Model.ValueObjects;
using TaskFlow.Domain.Shared.Enums;

namespace TaskFlow.Application.Mappers;

public static class TaskItemMapper
{
    // ===== Entity → DTO =====
    public static TaskItemDto ToDto(this TaskItem entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        Title = entity.Title,
        Description = entity.Description,
        Priority = entity.Priority,
        Status = entity.Status,
        Features = entity.Features,
        EstimatedEffort = entity.EstimatedEffort,
        ActualEffort = entity.ActualEffort,
        CompletedDate = entity.CompletedDate,
        CategoryId = entity.CategoryId,
        ParentTaskItemId = entity.ParentTaskItemId,
        StartDate = entity.DateRange.StartDate,
        DueDate = entity.DateRange.DueDate,
        RecurrenceInterval = entity.RecurrencePattern != null ? entity.RecurrencePattern.Interval : null,
        RecurrenceFrequency = entity.RecurrencePattern != null ? entity.RecurrencePattern.Frequency : null,
        RecurrenceEndDate = entity.RecurrencePattern != null ? entity.RecurrencePattern.EndDate : null,
        Comments = entity.Comments.Select(c => c.ToDto()).ToList(),
        ChecklistItems = entity.ChecklistItems.Select(ci => ci.ToDto()).ToList(),
        Tags = entity.TaskItemTags.Select(tt => tt.Tag!.ToDto()).ToList(),
        SubTasks = entity.SubTasks.Select(s => s.ToDto()).ToList(),
        CategoryName = entity.Category?.Name
    };

    // ===== DTO → Entity =====
    public static DomainResult<TaskItem> ToEntity(this TaskItemDto dto, Guid tenantId)
    {
        var result = TaskItem.Create(tenantId, dto.Title, dto.Description, dto.Priority, dto.CategoryId, dto.ParentTaskItemId);
        if (result.IsFailure) return result;

        var entity = result.Value!;
        entity.UpdateDateRange(dto.StartDate, dto.DueDate);

        if (dto.RecurrenceInterval.HasValue && !string.IsNullOrEmpty(dto.RecurrenceFrequency))
        {
            entity.UpdateRecurrencePattern(new RecurrencePattern
            {
                Interval = dto.RecurrenceInterval.Value,
                Frequency = dto.RecurrenceFrequency!,
                EndDate = dto.RecurrenceEndDate
            });
        }

        return DomainResult<TaskItem>.Success(entity);
    }

    // ===== EF-Safe Projectors =====

    public static readonly Expression<Func<TaskItem, TaskItemDto>> ProjectorSearch =
        entity => new TaskItemDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Title = entity.Title,
            Description = entity.Description,
            Priority = entity.Priority,
            Status = entity.Status,
            Features = entity.Features,
            EstimatedEffort = entity.EstimatedEffort,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category != null ? entity.Category.Name : null,
            StartDate = entity.DateRange.StartDate,
            DueDate = entity.DateRange.DueDate,
            CompletedDate = entity.CompletedDate
        };

    public static readonly Expression<Func<TaskItem, TaskItemDto>> ProjectorFull =
        entity => new TaskItemDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Title = entity.Title,
            Description = entity.Description,
            Priority = entity.Priority,
            Status = entity.Status,
            Features = entity.Features,
            EstimatedEffort = entity.EstimatedEffort,
            ActualEffort = entity.ActualEffort,
            CompletedDate = entity.CompletedDate,
            CategoryId = entity.CategoryId,
            ParentTaskItemId = entity.ParentTaskItemId,
            CategoryName = entity.Category != null ? entity.Category.Name : null,
            StartDate = entity.DateRange.StartDate,
            DueDate = entity.DateRange.DueDate,
            RecurrenceInterval = entity.RecurrencePattern != null ? entity.RecurrencePattern.Interval : null,
            RecurrenceFrequency = entity.RecurrencePattern != null ? entity.RecurrencePattern.Frequency : null,
            RecurrenceEndDate = entity.RecurrencePattern != null ? entity.RecurrencePattern.EndDate : null,
            Comments = entity.Comments.Select(c => new CommentDto
            {
                Id = c.Id,
                TenantId = c.TenantId,
                Body = c.Body,
                TaskItemId = c.TaskItemId
            }).ToList(),
            ChecklistItems = entity.ChecklistItems.Select(ci => new ChecklistItemDto
            {
                Id = ci.Id,
                TenantId = ci.TenantId,
                Title = ci.Title,
                IsCompleted = ci.IsCompleted,
                SortOrder = ci.SortOrder,
                CompletedDate = ci.CompletedDate,
                TaskItemId = ci.TaskItemId
            }).ToList(),
            SubTasks = entity.SubTasks.Select(s => new TaskItemDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                Title = s.Title,
                Status = s.Status,
                Priority = s.Priority
            }).ToList()
        };
}
