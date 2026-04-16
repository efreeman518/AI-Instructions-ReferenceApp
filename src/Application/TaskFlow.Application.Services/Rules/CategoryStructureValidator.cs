using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

internal static class CategoryStructureValidator
{
    public static Result<CategoryDto> ValidateCreate(CategoryDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<CategoryDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Category name is required.");
        if (dto.Name?.Length > DomainConstants.RULE_CATEGORY_NAME_LENGTH_MAX) errors.Add($"Category name cannot exceed {DomainConstants.RULE_CATEGORY_NAME_LENGTH_MAX} characters.");
        if (dto.Description?.Length > DomainConstants.RULE_CATEGORY_DESCRIPTION_LENGTH_MAX) errors.Add($"Description cannot exceed {DomainConstants.RULE_CATEGORY_DESCRIPTION_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<CategoryDto>.Failure(errors) : Result<CategoryDto>.Success(dto);
    }

    public static Result<CategoryDto> ValidateUpdate(CategoryDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<CategoryDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Category name is required.");
        if (dto.Name?.Length > DomainConstants.RULE_CATEGORY_NAME_LENGTH_MAX) errors.Add($"Category name cannot exceed {DomainConstants.RULE_CATEGORY_NAME_LENGTH_MAX} characters.");
        if (dto.Description?.Length > DomainConstants.RULE_CATEGORY_DESCRIPTION_LENGTH_MAX) errors.Add($"Description cannot exceed {DomainConstants.RULE_CATEGORY_DESCRIPTION_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<CategoryDto>.Failure(errors) : Result<CategoryDto>.Success(dto);
    }
}
