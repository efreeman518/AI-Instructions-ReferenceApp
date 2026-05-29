using EF.Common.Contracts;
using TaskFlow.Application.Cqrs.Shared;
using TaskFlow.Application.Models;
using TaskFlow.Domain.Shared.Constants;

namespace TaskFlow.Application.Cqrs.Features.Comments;

/// <summary>Provides comment structure validator behavior for the Features Comments layer.</summary>
internal static class CommentStructureValidator
{
    /// <summary>Validates validate create rules and returns failures before work continues.</summary>
    public static Result<CommentDto> ValidateCreate(CommentDto dto)
    {
        var common = StructureValidators.ValidateCreate(dto);
        if (common.IsFailure) return Result<CommentDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Body)) errors.Add("Body is required.");
        if (dto.Body?.Length > DomainConstants.RULE_COMMENT_BODY_LENGTH_MAX) errors.Add($"Body cannot exceed {DomainConstants.RULE_COMMENT_BODY_LENGTH_MAX} characters.");
        if (dto.TaskItemId == Guid.Empty) errors.Add("TaskItemId is required.");
        return errors.Count > 0 ? Result<CommentDto>.Failure(errors) : Result<CommentDto>.Success(dto);
    }

    /// <summary>Validates validate update rules and returns failures before work continues.</summary>
    public static Result<CommentDto> ValidateUpdate(CommentDto dto)
    {
        var common = StructureValidators.ValidateUpdate(dto);
        if (common.IsFailure) return Result<CommentDto>.Failure(common.ErrorMessage!);

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Body)) errors.Add("Body is required.");
        if (dto.Body?.Length > DomainConstants.RULE_COMMENT_BODY_LENGTH_MAX) errors.Add($"Body cannot exceed {DomainConstants.RULE_COMMENT_BODY_LENGTH_MAX} characters.");
        return errors.Count > 0 ? Result<CommentDto>.Failure(errors) : Result<CommentDto>.Success(dto);
    }
}
