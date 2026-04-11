using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class ChecklistItemMapper
{
    public static ChecklistItemDto ToDto(this ChecklistItem entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        IsCompleted = entity.IsCompleted,
        SortOrder = entity.SortOrder,
        CompletedDate = entity.CompletedDate,
        TaskItemId = entity.TaskItemId
    };

    public static DomainResult<ChecklistItem> ToEntity(this ChecklistItemDto dto, Guid tenantId)
        => ChecklistItem.Create(tenantId, dto.TaskItemId, dto.Title, dto.SortOrder);
}
