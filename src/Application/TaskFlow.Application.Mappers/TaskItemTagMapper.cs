using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

/// <summary>Maps task item tag mapper domain objects and DTOs across application boundaries.</summary>
public static class TaskItemTagMapper
{
    /// <summary>Converts the current value to DTO.</summary>
    public static TaskItemTagDto ToDto(this TaskItemTag entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        TaskItemId = entity.TaskItemId,
        TagId = entity.TagId
    };

    /// <summary>Converts the current value to entity.</summary>
    public static DomainResult<TaskItemTag> ToEntity(this TaskItemTagDto dto, Guid tenantId)
        => TaskItemTag.Create(tenantId, dto.TaskItemId, dto.TagId);
}
