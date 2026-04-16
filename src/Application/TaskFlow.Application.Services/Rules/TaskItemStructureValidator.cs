using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

internal static class TaskItemStructureValidator
{
    public static Result<TaskItemDto> ValidateCreate(TaskItemDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<TaskItemDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX) errors.Add($"Title cannot exceed {DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX} characters.");
        if (dto.Description?.Length > DomainConstants.RULE_DEFAULT_DESCRIPTION_LENGTH_MAX) errors.Add($"Description cannot exceed {DomainConstants.RULE_DEFAULT_DESCRIPTION_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<TaskItemDto>.Failure(errors) : Result<TaskItemDto>.Success(dto);
    }

    public static Result<TaskItemDto> ValidateUpdate(TaskItemDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<TaskItemDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX) errors.Add($"Title cannot exceed {DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX} characters.");
        if (dto.Description?.Length > DomainConstants.RULE_DEFAULT_DESCRIPTION_LENGTH_MAX) errors.Add($"Description cannot exceed {DomainConstants.RULE_DEFAULT_DESCRIPTION_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<TaskItemDto>.Failure(errors) : Result<TaskItemDto>.Success(dto);
    }
}
