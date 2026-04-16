using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

internal static class TagStructureValidator
{
    public static Result<TagDto> ValidateCreate(TagDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<TagDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Tag name is required.");
        if (dto.Name?.Length > DomainConstants.RULE_TAG_NAME_LENGTH_MAX) errors.Add($"Tag name cannot exceed {DomainConstants.RULE_TAG_NAME_LENGTH_MAX} characters.");
        if (dto.Color?.Length > DomainConstants.RULE_TAG_COLOR_LENGTH_MAX) errors.Add($"Color cannot exceed {DomainConstants.RULE_TAG_COLOR_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<TagDto>.Failure(errors) : Result<TagDto>.Success(dto);
    }

    public static Result<TagDto> ValidateUpdate(TagDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<TagDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Tag name is required.");
        if (dto.Name?.Length > DomainConstants.RULE_TAG_NAME_LENGTH_MAX) errors.Add($"Tag name cannot exceed {DomainConstants.RULE_TAG_NAME_LENGTH_MAX} characters.");
        if (dto.Color?.Length > DomainConstants.RULE_TAG_COLOR_LENGTH_MAX) errors.Add($"Color cannot exceed {DomainConstants.RULE_TAG_COLOR_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<TagDto>.Failure(errors) : Result<TagDto>.Success(dto);
    }
}
