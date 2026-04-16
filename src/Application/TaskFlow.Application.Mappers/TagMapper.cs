using System.Linq.Expressions;
using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class TagMapper
{
    public static TagDto ToDto(this Tag entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        Name = entity.Name,
        Color = entity.Color
    };

    public static DomainResult<Tag> ToEntity(this TagDto dto, Guid tenantId)
        => Tag.Create(tenantId, dto.Name, dto.Color);

    public static readonly Expression<Func<Tag, TagDto>> ProjectorSearch =
        entity => new TagDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            Name = entity.Name,
            Color = entity.Color
        };
}
