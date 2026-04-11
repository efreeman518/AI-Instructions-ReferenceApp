using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class CategoryStructureValidator
{
    public static Result<CategoryDto> ValidateCreate(CategoryDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Category name is required.");
        if (dto.Name?.Length > 100) errors.Add("Category name cannot exceed 100 characters.");
        if (dto.Description?.Length > 500) errors.Add("Description cannot exceed 500 characters.");
        return errors.Count > 0 ? Result<CategoryDto>.Failure(errors) : Result<CategoryDto>.Success(dto);
    }

    public static Result<CategoryDto> ValidateUpdate(CategoryDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("Category Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Category name is required.");
        if (dto.Name?.Length > 100) errors.Add("Category name cannot exceed 100 characters.");
        if (dto.Description?.Length > 500) errors.Add("Description cannot exceed 500 characters.");
        return errors.Count > 0 ? Result<CategoryDto>.Failure(errors) : Result<CategoryDto>.Success(dto);
    }
}
