using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

/// <summary>Maps tag mapper domain objects and DTOs across application boundaries.</summary>
public static class TagMapper
{
    public static readonly Expression<Func<Tag, TagDto>> Projection =
        entity => new TagDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Color = entity.Color
        };

    private static readonly Func<Tag, TagDto> Compiled = Projection.Compile();

    /// <summary>Converts the current value to DTO.</summary>
    public static TagDto ToDto(this Tag entity) => Compiled(entity);

    /// <summary>Converts the current value to entity.</summary>
    public static DomainResult<Tag> ToEntity(this TagDto dto, Guid tenantId)
        => Tag.Create(tenantId, dto.Name, dto.Color);
}
