using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class ChecklistItemMapper
{
    // Canonical full shape. EF translates this server-side; the same expression is compiled
    // once at static init and reused for in-memory ToDto so the two call sites cannot drift.
    public static readonly Expression<Func<ChecklistItem, ChecklistItemDto>> Projection =
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

    private static readonly Func<ChecklistItem, ChecklistItemDto> Compiled = Projection.Compile();

    public static ChecklistItemDto ToDto(this ChecklistItem entity) => Compiled(entity);

    public static DomainResult<ChecklistItem> ToEntity(this ChecklistItemDto dto, Guid tenantId)
        => ChecklistItem.Create(tenantId, dto.TaskItemId, dto.Title, dto.SortOrder);
}
