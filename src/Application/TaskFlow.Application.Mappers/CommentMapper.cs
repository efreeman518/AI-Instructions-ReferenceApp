using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

/// <summary>Maps comment mapper domain objects and DTOs across application boundaries.</summary>
public static class CommentMapper
{
    public static readonly Expression<Func<Comment, CommentDto>> Projection =
        entity => new CommentDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Body = entity.Body,
            TaskItemId = entity.TaskItemId
        };

    private static readonly Func<Comment, CommentDto> Compiled = Projection.Compile();

    /// <summary>Converts the current value to DTO.</summary>
    public static CommentDto ToDto(this Comment entity) => Compiled(entity);

    /// <summary>Converts the current value to entity.</summary>
    public static DomainResult<Comment> ToEntity(this CommentDto dto, Guid tenantId)
        => Comment.Create(tenantId, dto.TaskItemId, dto.Body);
}
