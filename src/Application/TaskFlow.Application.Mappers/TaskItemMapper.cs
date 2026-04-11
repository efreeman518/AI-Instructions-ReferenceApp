using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;
using TaskFlow.Domain.Model.ValueObjects;

namespace TaskFlow.Application.Mappers;

public static class TaskItemMapper
{
    public static TaskItemDto ToDto(this TaskItem entity) => new()
    {
        Id = entity.Id,
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
        StartDate = entity.DateRange?.StartDate,
        DueDate = entity.DateRange?.DueDate,
        RecurrenceInterval = entity.RecurrencePattern?.Interval,
        RecurrenceFrequency = entity.RecurrencePattern?.Frequency,
        RecurrenceEndDate = entity.RecurrencePattern?.EndDate
    };

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
}
