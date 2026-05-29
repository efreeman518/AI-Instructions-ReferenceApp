using EF.Common.Contracts;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Services.Rules;

/// <summary>Provides checklist item structure validator behavior for the Application Rules layer.</summary>
internal static class ChecklistItemStructureValidator
{
    /// <summary>Validates validate create rules and returns failures before work continues.</summary>
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

    /// <summary>Validates validate update rules and returns failures before work continues.</summary>
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
