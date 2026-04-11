using EF.Domain.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Model;

namespace TaskFlow.Application.Mappers;

public static class CategoryMapper
{
    public static CategoryDto ToDto(this Category entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        ParentCategoryId = entity.ParentCategoryId
    };

    public static DomainResult<Category> ToEntity(this CategoryDto dto, Guid tenantId)
        => Category.Create(tenantId, dto.Name, dto.Description, dto.SortOrder, dto.ParentCategoryId);
}
