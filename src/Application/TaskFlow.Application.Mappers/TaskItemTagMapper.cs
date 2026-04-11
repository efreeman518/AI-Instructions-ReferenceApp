using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class TaskItemTagMapper
{
    public static TaskItemTagDto ToDto(this TaskItemTag entity) => new()
    {
        Id = entity.Id,
        TaskItemId = entity.TaskItemId,
        TagId = entity.TagId
    };

    public static DomainResult<TaskItemTag> ToEntity(this TaskItemTagDto dto, Guid tenantId)
        => TaskItemTag.Create(tenantId, dto.TaskItemId, dto.TagId);
}
