using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class TagStructureValidator
{
    public static Result<TagDto> ValidateCreate(TagDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Tag name is required.");
        if (dto.Name?.Length > 50) errors.Add("Tag name cannot exceed 50 characters.");
        if (dto.Color?.Length > 7) errors.Add("Color cannot exceed 7 characters.");
        return errors.Count > 0 ? Result<TagDto>.Failure(errors) : Result<TagDto>.Success(dto);
    }

    public static Result<TagDto> ValidateUpdate(TagDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("Tag Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.Name)) errors.Add("Tag name is required.");
        if (dto.Name?.Length > 50) errors.Add("Tag name cannot exceed 50 characters.");
        if (dto.Color?.Length > 7) errors.Add("Color cannot exceed 7 characters.");
        return errors.Count > 0 ? Result<TagDto>.Failure(errors) : Result<TagDto>.Success(dto);
    }
}
