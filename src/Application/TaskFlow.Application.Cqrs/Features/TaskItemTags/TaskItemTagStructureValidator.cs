using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Cqrs.Features.TaskItemTags;

/// <summary>Provides task item tag structure validator behavior for the Features Task Item Tags layer.</summary>
internal static class TaskItemTagStructureValidator
{
    /// <summary>Validates validate create rules and returns failures before work continues.</summary>
    public static Result<TaskItemTagDto> ValidateCreate(TaskItemTagDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<TaskItemTagDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        if (dto.TagId == Guid.Empty) errors.Add("TagId is required.");
        return errors.Count > 0 ? Result<TaskItemTagDto>.Failure(errors) : Result<TaskItemTagDto>.Success(dto);
    }
}
