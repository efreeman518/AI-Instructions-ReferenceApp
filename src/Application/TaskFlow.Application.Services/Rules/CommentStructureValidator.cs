using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class CommentStructureValidator
{
    public static Result<CommentDto> ValidateCreate(CommentDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Body)) errors.Add("Body is required.");
        if (dto.Body?.Length > 2000) errors.Add("Body cannot exceed 2000 characters.");
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        return errors.Count > 0 ? Result<CommentDto>.Failure(errors) : Result<CommentDto>.Success(dto);
    }

    public static Result<CommentDto> ValidateUpdate(CommentDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("Comment Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.Body)) errors.Add("Body is required.");
        if (dto.Body?.Length > 2000) errors.Add("Body cannot exceed 2000 characters.");
        return errors.Count > 0 ? Result<CommentDto>.Failure(errors) : Result<CommentDto>.Success(dto);
    }
}
