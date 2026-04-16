using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

internal static class ChecklistItemStructureValidator
{
    public static Result<ChecklistItemDto> ValidateCreate(ChecklistItemDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<ChecklistItemDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX) errors.Add($"Title cannot exceed {DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX} characters.");
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        return errors.Count > 0 ? Result<ChecklistItemDto>.Failure(errors) : Result<ChecklistItemDto>.Success(dto);
    }

    public static Result<ChecklistItemDto> ValidateUpdate(ChecklistItemDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<ChecklistItemDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX) errors.Add($"Title cannot exceed {DomainConstants.RULE_DEFAULT_NAME_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<ChecklistItemDto>.Failure(errors) : Result<ChecklistItemDto>.Success(dto);
    }
}
