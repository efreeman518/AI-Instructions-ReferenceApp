using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Model.ValueObjects;

namespace TaskFlow.Application.Mappers;

public static class TaskItemMapper
{
    // Canonical full shape. EF translates this server-side; the same expression is compiled
    // once at static init and reused for in-memory ToDto so the two call sites cannot drift.
    //
    // Inline child projections (no .ToDto() calls): EF cannot translate method-call delegates,
    // so the Expression form must construct child DTOs directly. The MapperTests parity check
    // verifies the compiled result still agrees with each child mapper's ToDto.
    //
    // Owned-type flattening (DateRange / RecurrencePattern -> scalar columns) must stay
    // EF-translatable AND evaluate correctly in-memory — keep these to property access and
    // null-conditional checks only.
    public static readonly Expression<Func<TaskItem, TaskItemDto>> Projection =
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
            Tags = entity.TaskItemTags.Select(tt => new TagDto
            {
                Id = tt.Tag!.Id,
                TenantId = tt.Tag.TenantId,
                Name = tt.Tag.Name,
                Color = tt.Tag.Color
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

    private static readonly Func<TaskItem, TaskItemDto> Compiled = Projection.Compile();

    public static TaskItemDto ToDto(this TaskItem entity) => Compiled(entity);

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

    // Minimal query-only shape for list/search endpoints. Intentionally has no ToDto counterpart
    // — search results should never include children. Kept separate from Projection because
    // the two have different purposes (lean grid rows vs. full hydrated record).
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
}
