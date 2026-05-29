using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Cqrs.Features.Categories;

/// <summary>Provides category structure validator behavior for the Features Categories layer.</summary>
internal static class CategoryStructureValidator
{
    /// <summary>Validates validate create rules and returns failures before work continues.</summary>
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

    /// <summary>Validates validate update rules and returns failures before work continues.</summary>
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
