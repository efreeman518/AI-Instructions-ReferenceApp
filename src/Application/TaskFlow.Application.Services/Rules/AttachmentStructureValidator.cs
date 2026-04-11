using EF.Common.Contracts;
using TaskFlow.Application.Models;

namespace TaskFlow.Application.Services.Rules;

internal static class AttachmentStructureValidator
{
    public static Result<AttachmentDto> ValidateCreate(AttachmentDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.FileName)) errors.Add("FileName is required.");
        if (dto.FileName?.Length > 255) errors.Add("FileName cannot exceed 255 characters.");
        if (string.IsNullOrWhiteSpace(dto.ContentType)) errors.Add("ContentType is required.");
        if (dto.ContentType?.Length > 100) errors.Add("ContentType cannot exceed 100 characters.");
        if (dto.FileSizeBytes <= 0) errors.Add("FileSizeBytes must be greater than zero.");
        if (string.IsNullOrWhiteSpace(dto.StorageUri)) errors.Add("StorageUri is required.");
        if (dto.StorageUri?.Length > 2000) errors.Add("StorageUri cannot exceed 2000 characters.");
        if (dto.OwnerId == Guid.Empty) errors.Add("OwnerId is required.");
        return errors.Count > 0 ? Result<AttachmentDto>.Failure(errors) : Result<AttachmentDto>.Success(dto);
    }

    public static Result<AttachmentDto> ValidateUpdate(AttachmentDto dto)
    {
        var errors = new List<string>();
        if (dto.Id is null || dto.Id == Guid.Empty) errors.Add("Attachment Id is required for updates.");
        if (string.IsNullOrWhiteSpace(dto.FileName)) errors.Add("FileName is required.");
        if (dto.FileName?.Length > 255) errors.Add("FileName cannot exceed 255 characters.");
        if (string.IsNullOrWhiteSpace(dto.ContentType)) errors.Add("ContentType is required.");
        if (dto.ContentType?.Length > 100) errors.Add("ContentType cannot exceed 100 characters.");
        if (dto.FileSizeBytes <= 0) errors.Add("FileSizeBytes must be greater than zero.");
        if (string.IsNullOrWhiteSpace(dto.StorageUri)) errors.Add("StorageUri is required.");
        if (dto.StorageUri?.Length > 2000) errors.Add("StorageUri cannot exceed 2000 characters.");
        return errors.Count > 0 ? Result<AttachmentDto>.Failure(errors) : Result<AttachmentDto>.Success(dto);
    }
}
