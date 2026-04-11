using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class ChecklistItemStructureValidator
{
    public static Result<ChecklistItemDto> ValidateCreate(ChecklistItemDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > 200) errors.Add("Title cannot exceed 200 characters.");
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        return errors.Count > 0 ? Result<ChecklistItemDto>.Failure(errors) : Result<ChecklistItemDto>.Success(dto);
    }

    public static Result<ChecklistItemDto> ValidateUpdate(ChecklistItemDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("ChecklistItem Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.Title)) errors.Add("Title is required.");
        if (dto.Title?.Length > 200) errors.Add("Title cannot exceed 200 characters.");
        return errors.Count > 0 ? Result<ChecklistItemDto>.Failure(errors) : Result<ChecklistItemDto>.Success(dto);
    }
}
