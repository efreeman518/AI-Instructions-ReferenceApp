using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class TaskItemStructureValidator
{
    public static Result<TaskItemDto> ValidateCreate(TaskItemDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > 200) errors.Add("Title cannot exceed 200 characters.");
        if (dto.Description?.Length > 2000) errors.Add("Description cannot exceed 2000 characters.");
        return errors.Count > 0 ? Result<TaskItemDto>.Failure(errors) : Result<TaskItemDto>.Success(dto);
    }

    public static Result<TaskItemDto> ValidateUpdate(TaskItemDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("TaskItem Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > 200) errors.Add("Title cannot exceed 200 characters.");
        if (dto.Description?.Length > 2000) errors.Add("Description cannot exceed 2000 characters.");
        return errors.Count > 0 ? Result<TaskItemDto>.Failure(errors) : Result<TaskItemDto>.Success(dto);
    }
}
