using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class CommentMapper
{
    public static CommentDto ToDto(this Comment entity) => new()
    {
        Id = entity.Id,
        Body = entity.Body,
        TaskItemId = entity.TaskItemId
    };

    public static DomainResult<Comment> ToEntity(this CommentDto dto, Guid tenantId)
        => Comment.Create(tenantId, dto.TaskItemId, dto.Body);
}
