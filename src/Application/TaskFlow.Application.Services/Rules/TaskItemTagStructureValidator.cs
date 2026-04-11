using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class TaskItemTagStructureValidator
{
    public static Result<TaskItemTagDto> ValidateCreate(TaskItemTagDto dto)
    {
        var errors = new List<string>();
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        if (dto.TagId == Guid.Empty) errors.Add("TagId is required.");
        return errors.Count > 0 ? Result<TaskItemTagDto>.Failure(errors) : Result<TaskItemTagDto>.Success(dto);
    }
}
