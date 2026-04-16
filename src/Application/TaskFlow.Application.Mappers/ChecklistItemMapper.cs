using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class ChecklistItemMapper
{
    public static ChecklistItemDto ToDto(this ChecklistItem entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        Title = entity.Title,
        IsCompleted = entity.IsCompleted,
        SortOrder = entity.SortOrder,
        CompletedDate = entity.CompletedDate,
        TaskItemId = entity.TaskItemId
    };

    public static DomainResult<ChecklistItem> ToEntity(this ChecklistItemDto dto, Guid tenantId)
        => ChecklistItem.Create(tenantId, dto.TaskItemId, dto.Title, dto.SortOrder);

    public static readonly Expression<Func<ChecklistItem, ChecklistItemDto>> ProjectorSearch =
        entity => new ChecklistItemDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Title = entity.Title,
            IsCompleted = entity.IsCompleted,
            SortOrder = entity.SortOrder,
            CompletedDate = entity.CompletedDate,
            TaskItemId = entity.TaskItemId
        };
}
